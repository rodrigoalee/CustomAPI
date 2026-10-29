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

        //[INICIO][27/10/2026][Rodriale][Proveedor alterno: Twilio ("Twilio") si la cuenta de Meta queda bloqueada; por defecto, Meta]
        public string Proveedor { get; set; } = "Meta";

        public string TwilioAccountSid { get; set; } = string.Empty;

        public string TwilioAuthToken { get; set; } = string.Empty;

        public string TwilioNumero { get; set; } = string.Empty;

        public bool UsaTwilio => Proveedor.Equals("Twilio", StringComparison.OrdinalIgnoreCase);
        //[FIN][27/10/2026][Rodriale][Proveedor alterno: Twilio]

        //[INICIO][29/10/2026][Rodriale][Proveedor "Tigo": los avisos salen por SMS con la API B2B de Tigo Guatemala en lugar de WhatsApp]
        public string TigoUrlBase { get; set; } = "https://prod.api.tigo.com";

        public string TigoAutenticacion { get; set; } = string.Empty;

        public string TigoApiKey { get; set; } = string.Empty;

        public string TigoApiSecret { get; set; } = string.Empty;

        public string TigoOrganizacion { get; set; } = string.Empty;

        public string TigoShortcode { get; set; } = string.Empty;

        public string TigoTipoShortcode { get; set; } = "pretty_code";

        public bool UsaTigo => Proveedor.Equals("Tigo", StringComparison.OrdinalIgnoreCase);

        public bool Configurado => UsaTigo
            ? !string.IsNullOrWhiteSpace(TigoAutenticacion)
              && !string.IsNullOrWhiteSpace(TigoApiKey)
              && !string.IsNullOrWhiteSpace(TigoApiSecret)
              && !string.IsNullOrWhiteSpace(TigoOrganizacion)
              && !string.IsNullOrWhiteSpace(TigoShortcode)
            : UsaTwilio
            ? !string.IsNullOrWhiteSpace(TwilioAccountSid)
              && !string.IsNullOrWhiteSpace(TwilioAuthToken)
              && !string.IsNullOrWhiteSpace(TwilioNumero)
            : !string.IsNullOrWhiteSpace(Token)
              && !string.IsNullOrWhiteSpace(PhoneNumberId);
        //[FIN][29/10/2026][Rodriale][Proveedor "Tigo"]
    }
}
//[FIN][25/10/2026][Rodriale][Credenciales de la Cloud API de WhatsApp (Meta)]
