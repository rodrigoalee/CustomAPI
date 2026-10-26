//[INICIO][26/10/2026][Rodriale][DTOs de la pasarela de pago de prueba que se abre desde el enlace de WhatsApp]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos
{
    public record LineaResumenPagoDto(string Descripcion, int Cantidad, decimal Subtotal);

    public record ResumenPagoDto(
        int IdFactura,
        string NombreCliente,
        string Vehiculo,
        string Sucursal,
        IReadOnlyList<LineaResumenPagoDto> Lineas,
        decimal Subtotal,
        decimal MontoImpuesto,
        decimal Total,
        bool Pagada);

    public record PagarConTarjetaRequest(
        [Required, MaxLength(23)] string NumeroTarjeta,
        [Required, RegularExpression(@"^(0[1-9]|1[0-2])/\d{2}$", ErrorMessage = "El vencimiento debe tener el formato MM/AA.")]
        string Vencimiento,
        [Required, RegularExpression(@"^\d{3,4}$", ErrorMessage = "El CVC tiene 3 o 4 dígitos.")]
        string Cvc,
        [Required, MaxLength(100)] string Titular);

    public record ResultadoPagoDto(bool Pagado, string Mensaje);
}
//[FIN][26/10/2026][Rodriale][DTOs de la pasarela de pago de prueba]
