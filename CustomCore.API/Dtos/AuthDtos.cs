//[INICIO][16/9/2026][jgarciad8][DTOs para autenticación y consulta del usuario actual]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos;

//[INICIO][16/9/2026][jgarciad8][Validaciones del login con mensajes en español]
public record LoginRequest(
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo debe tener un formato válido.")]
    [MaxLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
    string Correo,

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MaxLength(72, ErrorMessage = "La contraseña no puede superar los 72 caracteres.")]
    string Password);
//[FIN][16/9/2026][jgarciad8][Validaciones del login con mensajes en español]


public record UsuarioSesionDto(
    int IdUsuario,
    string NombreCompleto,
    string Correo);


public record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiraEn,
    UsuarioSesionDto Usuario);
//[FIN][16/9/2026][jgarciad8][DTOs para autenticación y consulta del usuario actual]