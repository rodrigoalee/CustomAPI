//[INICIO][26/10/2026][Rodriale][Pasarela propia en modo de prueba: validamos la tarjeta nosotros y Stripe (test) procesa]
using System.Globalization;
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using CustomCore.API.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    /// <remarks>
    /// Solo acepta las tarjetas de prueba de Stripe y las traduce a sus métodos de pago de prueba,
    /// así ningún número real llega a Stripe. El número y el CVC nunca se guardan ni se registran.
    /// </remarks>
    public sealed class PasarelaPruebaService(
        AppDbContext db,
        EnlacePago enlaces,
        PagoStripeService stripe,
        NotificacionWhatsAppService whatsApp,
        SucursalActual sucursal)
    {
        private static readonly Dictionary<string, string> TarjetasDePrueba = new()
        {
            ["4242424242424242"] = "pm_card_visa",
            ["4000056655665556"] = "pm_card_visa_debit",
            ["5555555555554444"] = "pm_card_mastercard",
            ["4000000000000002"] = "pm_card_chargeDeclined",
            ["4000000000009995"] = "pm_card_chargeDeclinedInsufficientFunds",
            ["4000000000000069"] = "pm_card_chargeDeclinedExpiredCard",
        };

        //[INICIO][26/10/2026][Rodriale][El cajero envía el enlace: la factura debe ser de su sede y estar pendiente]
        public async Task<bool> EnviarEnlaceAsync(int facturaId, CancellationToken cancellationToken)
        {
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            var factura = await db.FacturasEncabezado
                .AsNoTracking()
                .Where(f => f.IdFacturaEncabezado == facturaId && (alcance == null || f.OrdenTrabajo.SucursalId == alcance))
                .Select(f => new { f.OrdenTrabajoId, f.TotalPagado, f.EstadoPago })
                .FirstOrDefaultAsync(cancellationToken);

            if (factura is null)
                return false;

            if (factura.EstadoPago == EstadoPago.Pagado)
                throw new ConflictoNegocioException("Esta factura ya está pagada.");

            await whatsApp.FacturaListaAsync(factura.OrdenTrabajoId, factura.TotalPagado, enlaces.Crear(facturaId));
            return true;
        }
        //[FIN][26/10/2026][Rodriale][El cajero envía el enlace]

        public async Task<ResumenPagoDto?> ObtenerResumenAsync(string token, CancellationToken cancellationToken)
        {
            if (enlaces.Validar(token) is not { } facturaId)
                return null;

            return await db.FacturasEncabezado
                .AsNoTracking()
                .Where(f => f.IdFacturaEncabezado == facturaId)
                .Select(f => new ResumenPagoDto(
                    f.IdFacturaEncabezado,
                    f.NombreFacturacion,
                    f.OrdenTrabajo.Vehiculo.Marca + " " + f.OrdenTrabajo.Vehiculo.Modelo + " · " + f.OrdenTrabajo.Vehiculo.Placa,
                    db.Sucursales.Where(s => s.IdSucursal == f.OrdenTrabajo.SucursalId).Select(s => s.Nombre).First(),
                    f.Detalles
                        .OrderBy(d => d.IdFacturaDetalle)
                        .Select(d => new LineaResumenPagoDto(d.DescripcionItem, d.Cantidad, d.Subtotal))
                        .ToList(),
                    f.Subtotal,
                    f.MontoImpuesto,
                    f.TotalPagado,
                    f.EstadoPago == EstadoPago.Pagado))
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <returns>null si el enlace no es válido o venció.</returns>
        public async Task<ResultadoPagoDto?> PagarAsync(
            string token,
            PagarConTarjetaRequest request,
            CancellationToken cancellationToken)
        {
            if (enlaces.Validar(token) is not { } facturaId)
                return null;

            var numero = new string(request.NumeroTarjeta.Where(char.IsAsciiDigit).ToArray());

            if (!PasaLuhn(numero))
                return new ResultadoPagoDto(false, "El número de tarjeta no es válido. Revísalo.");

            if (Vencida(request.Vencimiento))
                return new ResultadoPagoDto(false, "La tarjeta está vencida.");

            if (!TarjetasDePrueba.TryGetValue(numero, out var metodoPago))
                return new ResultadoPagoDto(false, "Esta es una pasarela de prueba: usa una tarjeta de prueba, por ejemplo 4242 4242 4242 4242.");

            try
            {
                var resultado = await stripe.CobrarDesdeEnlaceAsync(facturaId, metodoPago, cancellationToken);
                return resultado is { Pagado: true }
                    ? new ResultadoPagoDto(true, "¡Pago aprobado! Gracias por tu preferencia.")
                    : new ResultadoPagoDto(false, "El banco no confirmó el pago. Intenta con otra tarjeta.");
            }
            catch (Stripe.StripeException ex) when (ex.StripeError?.Type == "card_error")
            {
                return new ResultadoPagoDto(false, MensajeDeRechazo(ex.StripeError.DeclineCode ?? ex.StripeError.Code));
            }
        }

        private static string MensajeDeRechazo(string? codigo) => codigo switch
        {
            "insufficient_funds" => "La tarjeta no tiene fondos suficientes. Intenta con otra.",
            "expired_card" => "El banco reporta la tarjeta como vencida. Intenta con otra.",
            _ => "El banco rechazó la tarjeta. Intenta con otra.",
        };

        // Dígito verificador de las tarjetas: detecta errores de tecleo antes de ir al procesador.
        private static bool PasaLuhn(string numero)
        {
            if (numero.Length is < 13 or > 19)
                return false;

            var suma = 0;
            for (var i = 0; i < numero.Length; i++)
            {
                var digito = numero[numero.Length - 1 - i] - '0';
                if (i % 2 == 1)
                {
                    digito *= 2;
                    if (digito > 9)
                        digito -= 9;
                }
                suma += digito;
            }
            return suma % 10 == 0;
        }

        // "MM/AA": la tarjeta sirve hasta el último día de ese mes.
        private static bool Vencida(string vencimiento)
        {
            var mes = int.Parse(vencimiento[..2], CultureInfo.InvariantCulture);
            var anio = 2000 + int.Parse(vencimiento[3..], CultureInfo.InvariantCulture);
            var hoy = DateTime.UtcNow;
            return anio < hoy.Year || (anio == hoy.Year && mes < hoy.Month);
        }
    }
}
//[FIN][26/10/2026][Rodriale][Pasarela propia en modo de prueba]
