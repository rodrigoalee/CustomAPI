//[INICIO][16/9/2026][jgarciad8][DTOs del módulo de usuarios]
namespace CustomCore.API.Dtos;

public record UsuarioResumenDto(
    int IdUsuario,
    string NombreCompleto,
    string Correo,
    bool Activo);
//[FIN][16/9/2026][jgarciad8][DTOs del módulo de usuarios]