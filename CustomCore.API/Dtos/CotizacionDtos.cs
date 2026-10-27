//[INICIO][26/10/2026][Rodriale][DTOs de cotizaciones: el precio lo escribe el taller porque el trabajo está fuera del catálogo]
using System.ComponentModel.DataAnnotations;
using CustomCore.API.Entidades;

namespace CustomCore.API.Dtos
{
    public record LineaCotizacionDto(
        string Descripcion,
        int Cantidad,
        decimal PrecioUnitario,
        decimal Subtotal);

    public record CotizacionDto(
        int IdCotizacion,
        EstadoCotizacion Estado,
        string? Nota,
        decimal Total,
        DateTimeOffset FechaCreacion,
        DateTimeOffset? FechaRespuesta,
        IReadOnlyList<LineaCotizacionDto> Lineas);

    public record CrearLineaCotizacionRequest(
        [Required(ErrorMessage = "Cada línea necesita un concepto.")]
        [MaxLength(200, ErrorMessage = "El concepto admite hasta 200 caracteres.")]
        string Descripcion,

        [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000.")]
        int Cantidad,

        [Range(typeof(decimal), "0", "1000000", ErrorMessage = "El precio debe estar entre 0 y 1,000,000.")]
        decimal PrecioUnitario);

    public record CrearCotizacionRequest(
        [MaxLength(500, ErrorMessage = "La nota admite hasta 500 caracteres.")]
        string? Nota,

        [Required(ErrorMessage = "Agrega al menos una línea.")]
        [MinLength(1, ErrorMessage = "Agrega al menos una línea.")]
        [MaxLength(20, ErrorMessage = "Una cotización admite hasta 20 líneas.")]
        IReadOnlyList<CrearLineaCotizacionRequest> Lineas);

    public record ResponderCotizacionRequest(
        [Required(ErrorMessage = "Indica si aceptas la cotización.")]
        bool? Aceptar);
}
//[FIN][26/10/2026][Rodriale][DTOs de cotizaciones]
