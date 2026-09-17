//[INICIO][16/9/2026][jgarciad8][Opciones de configuración para JWT]
namespace CustomCore.API.Configuracion;

public sealed class OpcionesJwt
{
    public const string Seccion = "Jwt";

    public string Clave { get; set; } = string.Empty;

   
    public string Emisor { get; set; } = string.Empty;
    public string Audiencia { get; set; } = string.Empty;

    public int DuracionMinutos { get; set; } = 60;
}
//[FIN][16/9/2026][jgarciad8][Opciones de configuración para JWT]