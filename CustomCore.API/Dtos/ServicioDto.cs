using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos
{
    public record ServicioDto(
        int IdServicio,
        string Nombre,
        decimal TarifaManoObra,
        bool Activo);

    public record CrearServicioRequest(
        [Required, MaxLength(100)] string Nombre,
        [Range(0, 9_999_999.99)] decimal TarifaManoObra);

    public record ActualizarServicioRequest(
        [Required, MaxLength(100)] string Nombre,
        [Range(0, 9_999_999.99)] decimal TarifaManoObra,
        bool Activo);
}