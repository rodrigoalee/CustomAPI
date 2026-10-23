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


//[INICIO][23/10/2026][Rodriale][PerfilCompleto es ClienteId no nulo: una cuenta de Google nace sin ficha porque Cliente.Telefono es obligatorio y Google no lo da]
public record UsuarioSesionDto(
    int IdUsuario,
    string NombreCompleto,
    string Correo,
    bool PerfilCompleto);
//[FIN][23/10/2026][Rodriale][PerfilCompleto es ClienteId no nulo]


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

//[INICIO][14/10/2026][Rodriale][Alta pública del cliente: lo mínimo para facturarle y avisarle. Sin roles ni sucursal, o cualquiera se registraría como gerente]
public record RegistroRequest(
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    string NombreCompleto,

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo debe tener un formato válido.")]
    [MaxLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
    string Correo,

    //[INICIO][14/10/2026][Rodriale][E.164 desde el registro: es el formato que exige WhatsApp, no se arregla después]
    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [RegularExpression(@"^\+[1-9]\d{7,14}$",
        ErrorMessage = "El teléfono debe incluir el código de país, por ejemplo +50255551234.")]
    [MaxLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    string Telefono,
    //[FIN][14/10/2026][Rodriale][E.164 desde el registro]

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MaxLength(72, ErrorMessage = "La contraseña no puede superar los 72 caracteres.")]
    string Password
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
//[FIN][14/10/2026][Rodriale][Alta pública del cliente]

//[INICIO][23/10/2026][Rodriale][El id_token que el front recibe de Google; ronda el kilobyte y el techo evita cuerpos enormes]
public record GoogleLoginRequest(
    [Required(ErrorMessage = "El id_token es obligatorio.")]
    [MaxLength(4096, ErrorMessage = "El id_token no puede superar los 4096 caracteres.")]
    string IdToken);
//[FIN][23/10/2026][Rodriale][El id_token que el front recibe de Google]

//[FIN][16/9/2026][jgarciad8][DTOs para autenticación y consulta del usuario actual]