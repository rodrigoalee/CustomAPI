//[INICIO][27/10/2026][Rodriale][DTOs de reportería: el filtro común y la tabla que se vuelca a Excel o PDF]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos;

public enum TipoReporte
{
    Ventas,
    Ordenes,
    Inventario,
    Citas
}

public enum FormatoReporte
{
    Excel,
    Pdf
}

public record ConsultaReporte : IValidatableObject
{
    private const int MaxDiasRango = 366;

    public FormatoReporte Formato { get; init; } = FormatoReporte.Excel;
    public DateOnly? Desde { get; init; }
    public DateOnly? Hasta { get; init; }

    [Range(1, int.MaxValue)]
    public int? SucursalId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Desde is { } desde && Hasta is { } hasta)
        {
            if (hasta < desde)
                yield return new ValidationResult("La fecha final no puede ser anterior a la inicial.", [nameof(Hasta)]);
            else if (hasta.DayNumber - desde.DayNumber > MaxDiasRango)
                yield return new ValidationResult("El rango no puede pasar de un año.", [nameof(Hasta)]);
        }
    }
}

public enum TipoColumna
{
    Texto,
    Fecha,
    Entero,
    Moneda
}

public record ColumnaReporte(string Titulo, TipoColumna Tipo = TipoColumna.Texto);

public record TablaReporte(
    string Titulo,
    string Subtitulo,
    IReadOnlyList<ColumnaReporte> Columnas,
    IReadOnlyList<object?[]> Filas);

public record ArchivoReporte(byte[] Contenido, string TipoContenido, string Nombre);
//[FIN][27/10/2026][Rodriale][DTOs de reportería]
