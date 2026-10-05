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
        private const string EventoPagoExitoso = "payment_intent.succeeded";
        private const string ClaveMetadataFactura = "facturaId";

        private readonly OpcionesStripe _opciones = opciones.Value;

        //[INICIO][16/9/2026][Rodriale][Lo mínimo de una factura para cobrarla; se valida una sola vez y lo usan tanto Checkout como el cobro directo]
        private sealed record FacturaCobrable(int IdFactura, int OrdenTrabajoId, decimal Total, string Placa);
        //[FIN][16/9/2026][Rodriale][Lo mínimo de una factura para cobrarla]

        //[INICIO][16/9/2026][Rodriale][Crea la página de pago de Stripe para una factura pendiente y devuelve su enlace]
        public async Task<SesionPagoDto?> CrearSesionAsync(int facturaId, CancellationToken cancellationToken)
        {
            var factura = await ObtenerFacturaPendienteAsync(facturaId, cancellationToken);

            if (factura is null)
                return null;

            var metadata = MetadataDe(factura);

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
                            UnitAmount = ACentavos(factura.Total),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Factura #{factura.IdFactura} · Los Santos Customs",
                                Description = DescripcionDe(factura)
                            }
                        }
                    }
                ],
                //[FIN][16/9/2026][Rodriale][Un solo renglón con el total de la factura]

                //[INICIO][16/9/2026][Rodriale][La factura viaja en la sesión y también en el pago interno que crea Checkout, así cualquiera de los dos eventos del webhook la identifica]
                ClientReferenceId = factura.IdFactura.ToString(),
                Metadata = metadata,
                PaymentIntentData = new SessionPaymentIntentDataOptions { Metadata = metadata }
                //[FIN][16/9/2026][Rodriale][La factura viaja en la sesión y también en el pago interno]
            };

            var sesion = await new SessionService(stripe).CreateAsync(
                opcionesSesion,
                cancellationToken: cancellationToken);

            return new SesionPagoDto(sesion.Url, sesion.Id);
        }
        //[FIN][16/9/2026][Rodriale][Crea la página de pago de Stripe]

        //[INICIO][16/9/2026][Rodriale][Cobro directo con interfaz propia: el frontend manda el pm_ que generó Stripe Elements y el servidor crea y confirma el pago en una sola llamada]
        public async Task<ResultadoCobroDto?> CobrarAsync(
            int facturaId,
            CobrarFacturaRequest request,
            CancellationToken cancellationToken)
        {
            var factura = await ObtenerFacturaPendienteAsync(facturaId, cancellationToken);

            if (factura is null)
                return null;

            var opcionesPago = new PaymentIntentCreateOptions
            {
                Amount = ACentavos(factura.Total),
                Currency = _opciones.Moneda,
                PaymentMethod = request.MetodoPago,
                Confirm = true,
                Description = $"Factura #{factura.IdFactura} · {DescripcionDe(factura)}",
                Metadata = MetadataDe(factura),

                //[INICIO][16/9/2026][Rodriale][Sin redirecciones: al confirmar desde el servidor no hay página a dónde volver, así que solo se aceptan métodos que se resuelven en el momento, como la tarjeta]
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                }
                //[FIN][16/9/2026][Rodriale][Sin redirecciones]
            };

            //[INICIO][16/9/2026][Rodriale][Si llega la misma petición dos veces (doble clic, reintento de red), Stripe devuelve el resultado de la primera en vez de cobrar otra vez]
            var opcionesPeticion = new RequestOptions
            {
                IdempotencyKey = $"cobro-factura-{factura.IdFactura}-{request.MetodoPago}"
            };
            //[FIN][16/9/2026][Rodriale][Si llega la misma petición dos veces]

            //[INICIO][16/9/2026][Rodriale][Una tarjeta rechazada lanza StripeException y el manejador global la traduce a 402]
            var pago = await new PaymentIntentService(stripe).CreateAsync(
                opcionesPago,
                opcionesPeticion,
                cancellationToken);
            //[FIN][16/9/2026][Rodriale][Una tarjeta rechazada lanza StripeException]

            if (pago.Status == "succeeded")
            {
                await MarcarPagadaAsync(factura.IdFactura, pago.Id, cancellationToken);
                return new ResultadoCobroDto(pago.Id, pago.Status, Pagado: true, ClientSecret: null);
            }

            //[INICIO][16/9/2026][Rodriale][El banco pidió 3D Secure: el frontend usa el ClientSecret para mostrar la verificación y el webhook marca la factura cuando se apruebe]
            logger.LogInformation("Cobro de factura {Factura} quedó en estado {Estado}", factura.IdFactura, pago.Status);

            return new ResultadoCobroDto(
                pago.Id,
                pago.Status,
                Pagado: false,
                ClientSecret: pago.Status == "requires_action" ? pago.ClientSecret : null);
            //[FIN][16/9/2026][Rodriale][El banco pidió 3D Secure]
        }
        //[FIN][16/9/2026][Rodriale][Cobro directo con interfaz propia]

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
            //[FIN][16/9/2026][Rodriale][Si la firma no cuadra]

            //[INICIO][16/9/2026][Rodriale][Dos caminos llegan aquí: el pago por la página de Stripe y el cobro directo que se completó después de 3D Secure]
            switch (evento.Type)
            {
                case EventoSesionCompletada when evento.Data.Object is Session sesion:
                    if (sesion.PaymentStatus != "paid")
                    {
                        logger.LogInformation("Sesión {Sesion} completada pero sin pago confirmado ({Estado})",
                            sesion.Id, sesion.PaymentStatus);
                        return ResultadoWebhook.Ignorado;
                    }
                    return await MarcarDesdeMetadataAsync(sesion.Metadata, sesion.PaymentIntentId, cancellationToken);

                case EventoPagoExitoso when evento.Data.Object is PaymentIntent pago:
                    return await MarcarDesdeMetadataAsync(pago.Metadata, pago.Id, cancellationToken);

                default:
                    return ResultadoWebhook.Ignorado;
            }
            //[FIN][16/9/2026][Rodriale][Dos caminos llegan aquí]
        }
        //[FIN][16/9/2026][Rodriale][Recibe el aviso de Stripe]

        //[INICIO][16/9/2026][Rodriale][Valida que la factura exista y no esté pagada antes de pedirle nada a Stripe]
        private async Task<FacturaCobrable?> ObtenerFacturaPendienteAsync(int facturaId, CancellationToken cancellationToken)
        {
            var factura = await db.FacturasEncabezado
                .AsNoTracking()
                .Where(f => f.IdFacturaEncabezado == facturaId)
                .Select(f => new
                {
                    Datos = new FacturaCobrable(
                        f.IdFacturaEncabezado,
                        f.OrdenTrabajoId,
                        f.TotalPagado,
                        f.OrdenTrabajo.Vehiculo.Placa),
                    f.EstadoPago
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (factura is null)
                return null;

            if (factura.EstadoPago == EstadoPago.Pagado)
                throw new ConflictoNegocioException("Esta factura ya está pagada.");

            return factura.Datos;
        }
        //[FIN][16/9/2026][Rodriale][Valida que la factura exista y no esté pagada]

        private async Task<ResultadoWebhook> MarcarDesdeMetadataAsync(
            IDictionary<string, string>? metadata,
            string idPago,
            CancellationToken cancellationToken)
        {
            if (metadata is null ||
                !metadata.TryGetValue(ClaveMetadataFactura, out var valor) ||
                !int.TryParse(valor, out var facturaId))
            {
                logger.LogInformation("Pago {Pago} sin facturaId en la metadata; no corresponde a una factura", idPago);
                return ResultadoWebhook.Ignorado;
            }

            await MarcarPagadaAsync(facturaId, idPago, cancellationToken);
            return ResultadoWebhook.Procesado;
        }

        //[INICIO][16/9/2026][Rodriale][Idempotente: el cobro directo y el webhook pueden llegar a marcar la misma factura; la condición de no estar pagada evita tocarla dos veces]
        private async Task MarcarPagadaAsync(int facturaId, string idPago, CancellationToken cancellationToken)
        {
            var filasAfectadas = await db.FacturasEncabezado
                .Where(f => f.IdFacturaEncabezado == facturaId && f.EstadoPago != EstadoPago.Pagado)
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .ActualizarAuditadoAsync(db, facturaId, s => s
                    .SetProperty(f => f.EstadoPago, EstadoPago.Pagado)
                    .SetProperty(f => f.StripePaymentId, idPago),
                    cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

            if (filasAfectadas > 0)
                logger.LogInformation("Factura {Factura} marcada como pagada con {Pago}", facturaId, idPago);
            else
                logger.LogInformation("Factura {Factura} ya estaba pagada o no existe; sin cambios", facturaId);
        }
        //[FIN][16/9/2026][Rodriale][Idempotente]

        private static Dictionary<string, string> MetadataDe(FacturaCobrable factura) =>
            new() { [ClaveMetadataFactura] = factura.IdFactura.ToString() };

        private static string DescripcionDe(FacturaCobrable factura) =>
            $"Orden de trabajo #{factura.OrdenTrabajoId} · Vehículo {factura.Placa}";

        //[INICIO][16/9/2026][Rodriale][Stripe cobra en la unidad mínima de la moneda: Q1000.00 viaja como 100000 centavos]
        private static long ACentavos(decimal monto) =>
            (long)Math.Round(monto * 100m, 0, MidpointRounding.AwayFromZero);
        //[FIN][16/9/2026][Rodriale][Stripe cobra en la unidad mínima de la moneda]
    }
}
//[FIN][16/9/2026][Rodriale][Integración con Stripe]
