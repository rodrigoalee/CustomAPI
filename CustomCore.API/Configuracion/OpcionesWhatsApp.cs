//[INICIO][25/10/2026][Rodriale][Credenciales de la Cloud API de WhatsApp (Meta); el token va en user-secrets o en Azure, nunca en appsettings]
namespace CustomCore.API.Configuracion
{
    public sealed class OpcionesWhatsApp
    {
        public const string Seccion = "WhatsApp";

        public string Token { get; set; } = string.Empty;

        public string PhoneNumberId { get; set; } = string.Empty;

        public string VersionApi { get; set; } = "v25.0";

        //[INICIO][26/10/2026][Rodriale][Enlace al seguimiento del cliente en el front; {idCita} se reemplaza al armar el aviso]
        public string UrlSeguimiento { get; set; } = string.Empty;

        //[INICIO][26/10/2026][Rodriale][Enlace a la pasarela de pago; {token} se reemplaza por el enlace firmado de la factura]
        public string UrlPago { get; set; } = string.Empty;
        //[FIN][26/10/2026][Rodriale][Enlace a la pasarela de pago]
        //[FIN][26/10/2026][Rodriale][Enlace al seguimiento del cliente en el front]

        public bool Configurado =>
            !string.IsNullOrWhiteSpace(Token)
            && !string.IsNullOrWhiteSpace(PhoneNumberId);
    }
}
//[FIN][25/10/2026][Rodriale][Credenciales de la Cloud API de WhatsApp (Meta)]
