//[INICIO][28/10/2026][Rodriale][Credenciales SMTP para los avisos por correo; la contraseña de aplicación va en user-secrets o en Azure, nunca en appsettings]
namespace CustomCore.API.Configuracion
{
    public sealed class OpcionesCorreo
    {
        public const string Seccion = "Correo";

        public string Servidor { get; set; } = "smtp.gmail.com";

        public int Puerto { get; set; } = 587;

        public string Usuario { get; set; } = string.Empty;

        public string Contrasena { get; set; } = string.Empty;

        public string NombreRemitente { get; set; } = "Los Santos Customs";

        public bool Configurado =>
            !string.IsNullOrWhiteSpace(Servidor)
            && !string.IsNullOrWhiteSpace(Usuario)
            && !string.IsNullOrWhiteSpace(Contrasena);
    }
}
//[FIN][28/10/2026][Rodriale][Credenciales SMTP para los avisos por correo]
