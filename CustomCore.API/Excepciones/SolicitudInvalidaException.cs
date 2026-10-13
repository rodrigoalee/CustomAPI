//[INICIO][12/10/2026][Rodriale][A la petición le falta un dato que solo el cliente puede dar]
namespace CustomCore.API.Excepciones
{
    public sealed class SolicitudInvalidaException(string mensaje) : Exception(mensaje);
}
//[FIN][12/10/2026][Rodriale][A la petición le falta un dato que solo el cliente puede dar]
