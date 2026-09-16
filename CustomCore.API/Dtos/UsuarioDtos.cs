//[INICIO][16/9/2026][jgarciad8][DTOs del módulo de usuarios]
using System.ComponentModel.DataAnnotations;
using CustomCore.API.Seguridad;

namespace CustomCore.API.Dtos;

public record UsuarioResumenDto(
    int IdUsuario,
    string NombreCompleto,
    string Correo,
    bool Activo);
//[FIN][16/9/2026][jgarciad8][DTOs del módulo de usuarios]

//[INICIO][16/9/2026][jgarciad8][Datos y validaciones para crear usuarios]
public record CrearUsuarioRequest(
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    string NombreCompleto,

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo debe tener un formato válido.")]
    [MaxLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
    string Correo,

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MaxLength(72, ErrorMessage = "La contraseña no puede superar los 72 caracteres.")]
    string Password
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        // Reutiliza la misma política del servicio BCrypt.
        // El límite de BCrypt se mide en bytes, no solo en caracteres.
        if (!string.IsNullOrWhiteSpace(Password)
            && !PasswordService.EsValida(Password))
        {
            yield return new ValidationResult(
                "La contraseña debe tener al menos 12 caracteres, " +
                "no superar los 72 bytes en UTF-8 y no contener caracteres nulos.",
                new[] { nameof(Password) });
        }
    }
}
//[FIN][16/9/2026][jgarciad8][Datos y validaciones para crear usuarios]