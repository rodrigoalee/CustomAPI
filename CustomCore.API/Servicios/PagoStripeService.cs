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
    public sealed class PagoStripeService(
        AppDbContext db,
        IStripeClient stripe,
        IOptions<OpcionesStripe> opciones)
    {
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

                //[INICIO][16/9/2026][Rodriale][Así el webhook de la Fase 8.2 sabe qué factura marcar cuando Stripe confirme el pago]
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

        //[INICIO][16/9/2026][Rodriale][Stripe cobra en la unidad mínima de la moneda: Q1000.00 viaja como 100000 centavos]
        private static long ACentavos(decimal monto) =>
            (long)Math.Round(monto * 100m, 0, MidpointRounding.AwayFromZero);
        //[FIN][16/9/2026][Rodriale][Stripe cobra en la unidad mínima de la moneda]
    }
}
//[FIN][16/9/2026][Rodriale][Integración con Stripe]