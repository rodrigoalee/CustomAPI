//[INICIO][23/10/2026][Rodriale][Client ID de Google: es el mismo del front y no es secreto, pero sin él la API no sabe para quién se emitió un id_token]
namespace CustomCore.API.Configuracion;

public sealed class OpcionesGoogle
{
    public const string Seccion = "Google";

    public string ClientId { get; set; } = string.Empty;
}
//[FIN][23/10/2026][Rodriale][Client ID de Google]
