//[INICIO][16/9/2026][jgarciad8][DTOs para autenticación y consulta del usuario actual]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos;

public record LoginRequest(
    [Required, EmailAddress, MaxLength(100)] string Correo,
    [Required, MaxLength(72)] string Password);


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