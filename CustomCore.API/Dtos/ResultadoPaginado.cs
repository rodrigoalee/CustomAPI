namespace CustomCore.API.Dtos
{
    public record class ResultadoPaginado<T>(
        IReadOnlyList<T> Items,
        int Pagina,
        int Tamanio,
        int Total);
}
