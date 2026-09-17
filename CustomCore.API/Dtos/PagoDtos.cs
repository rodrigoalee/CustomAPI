//[INICIO][16/9/2026][Rodriale][DTOs del pago en línea con Stripe]
namespace CustomCore.API.Dtos
{
    //[INICIO][16/9/2026][Rodriale][El frontend redirige al cliente a UrlPago; IdSesion sirve para rastrear el intento en el panel de Stripe]
    public record SesionPagoDto(
        string UrlPago,
        string IdSesion);
    //[FIN][16/9/2026][Rodriale][El frontend redirige al cliente a UrlPago]
}
//[FIN][16/9/2026][Rodriale][DTOs del pago en línea con Stripe]