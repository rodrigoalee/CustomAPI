//[INICIO][16/9/2026][jgarciad8][DTOs para administrar permisos]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos;

public record PermisoDto(
    int IdPermiso,
    string NombreCodigo,
    string? Descripcion,
    string Modulo);

public record GuardarPermisoRequest(
    [Required(ErrorMessage = "El código es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El código admite hasta 50 caracteres.")]
    [RegularExpression(
        @"^[a-z][a-z0-9_]*\.[a-z][a-z0-9_]*$",
        ErrorMessage = "Usa un código como usuarios.leer, en minúsculas.")]
    string NombreCodigo,

    [MaxLength(100, ErrorMessage = "La descripción admite hasta 100 caracteres.")]
    string? Descripcion,

    [Required(ErrorMessage = "El módulo es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El módulo admite hasta 50 caracteres.")]
    string Modulo
);
//[FIN][16/9/2026][jgarciad8][DTOs para administrar permisos]