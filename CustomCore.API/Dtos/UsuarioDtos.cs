//[INICIO][16/9/2026][jgarciad8][DTOs del módulo de usuarios]
using System.ComponentModel.DataAnnotations;
using CustomCore.API.Seguridad;

namespace CustomCore.API.Dtos;

public record UsuarioResumenDto(
    int IdUsuario,
    string NombreCompleto,
    string Correo,
    bool Activo,
    int? SucursalId);
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
    string Password,

    //[INICIO][12/10/2026][Rodriale][Sede del empleado; se omite solo para quien tendrá sucursales.todas]
    [Range(1, int.MaxValue, ErrorMessage = "La sucursal no es válida.")]
    int? SucursalId = null
    //[FIN][12/10/2026][Rodriale][Sede del empleado]
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {

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

//[INICIO][16/9/2026][jgarciad8][Datos para actualizar información del usuario]
public record ActualizarUsuarioRequest(
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    string NombreCompleto,

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo debe tener un formato válido.")]
    [MaxLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
    string Correo,

    //[INICIO][12/10/2026][Rodriale][PUT reemplaza el recurso: omitir la sede deja al usuario sin sede]
    [Range(1, int.MaxValue, ErrorMessage = "La sucursal no es válida.")]
    int? SucursalId = null
    //[FIN][12/10/2026][Rodriale][PUT reemplaza el recurso]
);
//[FIN][16/9/2026][jgarciad8][Datos para actualizar información del usuario]

//[INICIO][16/9/2026][jgarciad8][Datos para activar o desactivar una cuenta]
public record CambiarEstadoUsuarioRequest(
    [Required(ErrorMessage = "Debes indicar si el usuario estará activo.")]
    bool? Activo
);
//[FIN][16/9/2026][jgarciad8][Datos para activar o desactivar una cuenta]