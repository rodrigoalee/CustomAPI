//[INICIO][28/8/2026][Rodriale][Envoltura genérica para respuestas paginadas]
namespace CustomCore.API.Dtos
{
    public record class ResultadoPaginado<T>(
        IReadOnlyList<T> Items,
        int Pagina,
        int Tamanio,
        int Total);
}
//[FIN][28/8/2026][Rodriale][Envoltura genérica para respuestas paginadas]