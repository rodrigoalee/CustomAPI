using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos
{
    public record RepuestoDto(
        int IdRepuesto,
        string CodigoStock,
        string Nombre,
        decimal PrecioUnitario,
        bool Activo
    );

    //[INICIO][28/8/2026][Rodriale][DTO para crear repuestos]
    public record CrearRepuestoRequest(
        [Required, MaxLength(50)] string CodigoStock,
        [Required, MaxLength(100)] string Nombre,
        [Range(0, 9_999_999.99)] decimal PrecioUnitario
    );
    //[FIN][28/8/2026][Rodriale][DTO para crear repuestos]

    //[INICIO][28/8/2026][Rodriale][DTO para actualizar repuestos]
    public record ActualizarRepuestoRequest(
        [Required, MaxLength(50)] string CodigoStock,
        [Required, MaxLength(100)] string Nombre,
        [Range(0, 9_999_999.99)] decimal PrecioUnitario,
        bool Activo
    );
    //[FIN][28/8/2026][Rodriale][DTO para actualizar repuestos]
}
