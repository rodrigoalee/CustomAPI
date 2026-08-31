//[INICIO][31/8/2026][Rodriale][la operación es válida en forma pero rompe una regla del taller]
namespace CustomCore.API.Excepciones
{
    public sealed class ConflictoNegocioException(string mensaje) : Exception(mensaje);
}
//[FIN][31/8/2026][Rodriale][la operación es válida en forma pero rompe una regla del taller]