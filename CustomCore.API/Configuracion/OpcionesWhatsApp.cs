//[INICIO][25/10/2026][Rodriale][Credenciales de la Cloud API de WhatsApp (Meta); el token va en user-secrets o en Azure, nunca en appsettings]
namespace CustomCore.API.Configuracion
{
    public sealed class OpcionesWhatsApp
    {
        public const string Seccion = "WhatsApp";

        public string Token { get; set; } = string.Empty;

        public string PhoneNumberId { get; set; } = string.Empty;

        public string VersionApi { get; set; } = "v25.0";

        public bool Configurado =>
            !string.IsNullOrWhiteSpace(Token)
            && !string.IsNullOrWhiteSpace(PhoneNumberId);
    }
}
//[FIN][25/10/2026][Rodriale][Credenciales de la Cloud API de WhatsApp (Meta)]
