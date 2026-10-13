//[INICIO][17/9/2026][Rodriale][DTOs del catálogo de sucursales]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos
{
    public record SucursalDto(
        int IdSucursal,
        string Nombre,
        string Direccion,
        string? Telefono,
        bool Activo);

    public record CrearSucursalRequest(
        [Required, MaxLength(100)] string Nombre,
        [Required, MaxLength(200)] string Direccion,
        [MaxLength(20)] string? Telefono);

    public record ActualizarSucursalRequest(
        [Required, MaxLength(100)] string Nombre,
        [Required, MaxLength(200)] string Direccion,
        [MaxLength(20)] string? Telefono,
        bool Activo);
}
//[FIN][17/9/2026][Rodriale][DTOs del catálogo de sucursales]