//[INICIO][28/8/2026][Rodriale][Parámetros de paginación acotados para no permitir consultas abusivas]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos;

public record ConsultaPaginada
{
    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, 100)]
    public int Tamanio { get; init; } = 20;

    [MaxLength(100)]
    public string? Busqueda { get; init; }
}
//[FIN][28/8/2026][Rodriale][Parámetros de paginación acotados para no permitir consultas abusivas]