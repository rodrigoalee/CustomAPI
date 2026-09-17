//[INICIO][28/8/2026][Rodriale][Parámetros de paginación acotados para no permitir consultas abusivas]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos;

public record ConsultaPaginada : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, 100)]
    public int Tamanio { get; init; } = 20;

    [MaxLength(100)]
    public string? Busqueda { get; init; }

    //[INICIO][17/9/2026][jgarciad8][Validación del desplazamiento de paginación]
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var desplazamiento = ((long)Pagina - 1) * Tamanio;

        if (desplazamiento > int.MaxValue)
        {
            yield return new ValidationResult(
                "La página solicitada supera el límite de paginación.",
                new[] { nameof(Pagina), nameof(Tamanio) });
        }
    }
    //[FIN][17/9/2026][jgarciad8][Validación del desplazamiento de paginación]
}
//[FIN][28/8/2026][Rodriale][Parámetros de paginación acotados para no permitir consultas abusivas]