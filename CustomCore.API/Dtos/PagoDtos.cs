//[INICIO][16/9/2026][Rodriale][DTOs del pago en línea con Stripe]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos
{
    //[INICIO][16/9/2026][Rodriale][El frontend redirige al cliente a UrlPago; IdSesion sirve para rastrear el intento en el panel de Stripe]
    public record SesionPagoDto(
        string UrlPago,
        string IdSesion);
    //[FIN][16/9/2026][Rodriale][El frontend redirige al cliente a UrlPago]

    //[INICIO][16/9/2026][Rodriale][Nunca datos de tarjeta: solo el identificador pm_ que genera Stripe Elements en el navegador, o una tarjeta de prueba como pm_card_visa]
    public record CobrarFacturaRequest(
        [Required, MaxLength(255)] string MetodoPago);
    //[FIN][16/9/2026][Rodriale][Nunca datos de tarjeta]

    //[INICIO][16/9/2026][Rodriale][ClientSecret solo viene cuando el banco pide verificación 3D Secure y el frontend debe completarla]
    public record ResultadoCobroDto(
        string IdPago,
        string Estado,
        bool Pagado,
        string? ClientSecret);
    //[FIN][16/9/2026][Rodriale][ClientSecret solo viene cuando el banco pide verificación]
}
//[FIN][16/9/2026][Rodriale][DTOs del pago en línea con Stripe]