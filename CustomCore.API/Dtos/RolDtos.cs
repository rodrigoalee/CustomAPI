//[INICIO][16/9/2026][jgarciad8][DTOs de roles]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos;

public record RolDto(int IdRol, string Nombre, string? Descripcion);

public record GuardarRolRequest(
    [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre admite hasta 50 caracteres.")]
    string Nombre,

    [MaxLength(200, ErrorMessage = "La descripción admite hasta 200 caracteres.")]
    string? Descripcion
);
//[FIN][16/9/2026][jgarciad8][DTOs de roles]