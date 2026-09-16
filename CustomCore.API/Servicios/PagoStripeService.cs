//[INICIO][16/9/2026][Rodriale][Integración con Stripe: separada de FacturaService porque es infraestructura, no regla de negocio]
using CustomCore.API.Configuracion;
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace CustomCore.API.Servicios
{
    //[INICIO][16/9/2026][Rodriale][Lo que el controlador necesita saber para responderle a Stripe]
    public enum ResultadoWebhook
    {
        Procesado,
        Ignorado,
        FirmaInvalida
    }
    //[FIN][16/9/2026][Rodriale][Lo que el controlador necesita saber para responderle a Stripe]

    public sealed class PagoStripeService(
        AppDbContext db,
        IStripeClient stripe,
        IOptions<OpcionesStripe> opciones,
        ILogger<PagoStripeService> logger)
    {
        private const string EventoSesionCompletada = "checkout.session.completed";

        private readonly OpcionesStripe _opciones = opciones.Value;

        //[INICIO][16/9/2026][Rodriale][Crea la página de pago de Stripe para una factura pendiente y devuelve su enlace]
        public async Task<SesionPagoDto?> CrearSesionAsync(int facturaId, CancellationToken cancellationToken)
        {
            var factura = await db.FacturasEncabezado
                .AsNoTracking()
                .Where(f => f.IdFacturaEncabezado == facturaId)
                .Select(f => new
                {
                    f.IdFacturaEncabezado,
                    f.OrdenTrabajoId,
                    f.NombreFacturacion,
                    f.TotalPagado,
                    f.EstadoPago,
                    Placa = f.OrdenTrabajo.Vehiculo.Placa
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (factura is null)
                return null;

            if (factura.EstadoPago == EstadoPago.Pagado)
                throw new ConflictoNegocioException("Esta factura ya está pagada.");

            var opcionesSesion = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = _opciones.UrlExito,
                CancelUrl = _opciones.UrlCancelacion,

                //[INICIO][16/9/2026][Rodriale][Un solo renglón con el total de la factura: si se copiaran las líneas, Stripe volvería a sumarlas y el redondeo del IVA podría no cuadrar con lo facturado]
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = _opciones.Moneda,
                            UnitAmount = ACentavos(factura.TotalPagado),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Factura #{factura.IdFacturaEncabezado} · Los Santos Customs",
                                Description = $"Orden de trabajo #{factura.OrdenTrabajoId} · Vehículo {factura.Placa}"
                            }
                        }
                    }
                ],
                //[FIN][16/9/2026][Rodriale][Un solo renglón con el total de la factura]

                //[INICIO][16/9/2026][Rodriale][Así el webhook sabe qué factura marcar cuando Stripe confirme el pago]
                ClientReferenceId = factura.IdFacturaEncabezado.ToString(),
                Metadata = new Dictionary<string, string>
                {
                    ["facturaId"] = factura.IdFacturaEncabezado.ToString()
                }
                //[FIN][16/9/2026][Rodriale][Así el webhook sabe qué factura marcar]
            };

            var sesion = await new SessionService(stripe).CreateAsync(
                opcionesSesion,
                cancellationToken: cancellationToken);

            return new SesionPagoDto(sesion.Url, sesion.Id);
        }
        //[FIN][16/9/2026][Rodriale][Crea la página de pago de Stripe]

        //[INICIO][16/9/2026][Rodriale][Recibe el aviso de Stripe; es la única prueba confiable de que el pago ocurrió, no la redirección del navegador]
        public async Task<ResultadoWebhook> ProcesarWebhookAsync(
            string cuerpo,
            string firma,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_opciones.SecretoWebhook))
                throw new InvalidOperationException("Falta el secreto del webhook de Stripe en la configuración.");

            Event evento;

            //[INICIO][16/9/2026][Rodriale][Si la firma no cuadra, alguien que no es Stripe está llamando al endpoint. throwOnApiVersionMismatch en false evita rechazar eventos legítimos solo porque la cuenta de Stripe usa otra versión de API que la librería]
            try
            {
                evento = EventUtility.ConstructEvent(
                    cuerpo,
                    firma,
                    _opciones.SecretoWebhook,
                    throwOnApiVersionMismatch: false);
            }
            catch (StripeException ex)
            {
                logger.LogWarning(ex, "Webhook de Stripe rechazado por firma inválida");
                return ResultadoWebhook.FirmaInvalida;
            }
            //[FIN][16/9/2026][Rodriale][Si la firma no cuadra, alguien que no es Stripe está llamando]

            //[INICIO][16/9/2026][Rodriale][Stripe manda muchos tipos de evento; solo nos importa el de pago completado]
            if (evento.Type != EventoSesionCompletada || evento.Data.Object is not Session sesion)
                return ResultadoWebhook.Ignorado;

            if (sesion.PaymentStatus != "paid")
            {
                logger.LogInformation("Sesión {Sesion} completada pero sin pago confirmado ({Estado})",
                    sesion.Id, sesion.PaymentStatus);
                return ResultadoWebhook.Ignorado;
            }
            //[FIN][16/9/2026][Rodriale][Solo nos importa el de pago completado]

            if (sesion.Metadata is null ||
                !sesion.Metadata.TryGetValue("facturaId", out var valor) ||
                !int.TryParse(valor, out var facturaId))
            {
                logger.LogWarning("Sesión {Sesion} pagada sin facturaId en la metadata", sesion.Id);
                return ResultadoWebhook.Ignorado;
            }

            //[INICIO][16/9/2026][Rodriale][Idempotente: si Stripe repite el evento, la condición de no estar pagada evita tocarla dos veces]
            var filasAfectadas = await db.FacturasEncabezado
                .Where(f => f.IdFacturaEncabezado == facturaId && f.EstadoPago != EstadoPago.Pagado)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(f => f.EstadoPago, EstadoPago.Pagado)
                    .SetProperty(f => f.StripePaymentId, sesion.PaymentIntentId),
                    cancellationToken);
            //[FIN][16/9/2026][Rodriale][Idempotente]

            if (filasAfectadas > 0)
                logger.LogInformation("Factura {Factura} marcada como pagada con {Pago}", facturaId, sesion.PaymentIntentId);
            else
                logger.LogInformation("Factura {Factura} ya estaba pagada o no existe; evento sin efecto", facturaId);

            return ResultadoWebhook.Procesado;
        }
        //[FIN][16/9/2026][Rodriale][Recibe el aviso de Stripe]

        //[INICIO][16/9/2026][Rodriale][Stripe cobra en la unidad mínima de la moneda: Q1000.00 viaja como 100000 centavos]
        private static long ACentavos(decimal monto) =>
            (long)Math.Round(monto * 100m, 0, MidpointRounding.AwayFromZero);
        //[FIN][16/9/2026][Rodriale][Stripe cobra en la unidad mínima de la moneda]
    }
}
//[FIN][16/9/2026][Rodriale][Integración con Stripe]