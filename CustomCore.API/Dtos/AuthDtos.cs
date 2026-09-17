//[INICIO][16/9/2026][jgarciad8][DTOs para autenticación y consulta del usuario actual]
using System.ComponentModel.DataAnnotations;
using CustomCore.API.Seguridad;

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

//[INICIO][17/9/2026][jgarciad8][Validación del cambio de contraseña]
public record CambiarPasswordRequest(
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    [MaxLength(72, ErrorMessage = "La contraseña actual admite hasta 72 caracteres.")]
    string PasswordActual,

    [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
    [MaxLength(72, ErrorMessage = "La contraseña nueva admite hasta 72 caracteres.")]
    string PasswordNueva
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(PasswordNueva)
            && !PasswordService.EsValida(PasswordNueva))
        {
            yield return new ValidationResult(
                "La contraseña nueva debe tener al menos 12 caracteres, " +
                "no superar los 72 bytes en UTF-8 y no contener caracteres nulos.",
                new[] { nameof(PasswordNueva) });
        }
    }
}
//[FIN][17/9/2026][jgarciad8][Validación del cambio de contraseña]

//[FIN][16/9/2026][jgarciad8][DTOs para autenticación y consulta del usuario actual]