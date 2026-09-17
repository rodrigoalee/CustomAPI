//[INICIO][16/9/2026][jgarciad8][Controlador de autenticación]
using System.IdentityModel.Tokens.Jwt;
using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers;

[ApiController]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(AuthService servicio) : ControllerBase
{
    //[INICIO][16/9/2026][jgarciad8][Endpoint público para iniciar sesión]
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var respuesta = await servicio.IniciarSesionAsync(
            request,
            cancellationToken);

        if (respuesta is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciales inválidas",
                detail: "Correo o contraseña incorrectos.");
        }

        return Ok(respuesta);
    }
    //[FIN][16/9/2026][jgarciad8][Endpoint público para iniciar sesión]

    //[INICIO][16/9/2026][jgarciad8][Endpoint protegido para consultar al usuario actual]
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UsuarioSesionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UsuarioSesionDto>> ObtenerUsuarioActual(
        CancellationToken cancellationToken)
    {

        if (!int.TryParse(
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            out var idUsuario))
        {
            return Unauthorized();
        }

        var usuario = await servicio.ObtenerUsuarioActualAsync(
            idUsuario,
            cancellationToken);

        return usuario is null ? Unauthorized() : Ok(usuario);
    }
    //[FIN][16/9/2026][jgarciad8][Endpoint protegido para consultar al usuario actual]

    //[INICIO][17/9/2026][jgarciad8][Cambio de contraseña de la cuenta autenticada]
    [Authorize]
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarPassword(
        CambiarPasswordRequest request,
        CancellationToken cancellationToken)
    {

        if (!int.TryParse(
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            out var idUsuario)
            || idUsuario <= 0)
        {
            return Unauthorized();
        }

        var resultado = await servicio.CambiarPasswordAsync(
            idUsuario, request, cancellationToken);

        return resultado switch
        {
            ResultadoCambioPassword.Exito => NoContent(),

            ResultadoCambioPassword.NoAutorizado => Unauthorized(),

            ResultadoCambioPassword.ActualIncorrecta => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Contraseña actual incorrecta",
                detail: "La contraseña actual no coincide."),

            ResultadoCambioPassword.NuevaIgual => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Contraseña sin cambios",
                detail: "La contraseña nueva debe ser diferente de la actual."),

            _ => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "La cuenta cambió durante la operación",
                detail: "Vuelve a iniciar sesión y comprueba el estado de tu cuenta.")
        };
    }
    //[FIN][17/9/2026][jgarciad8][Cambio de contraseña de la cuenta autenticada]
}
//[FIN][16/9/2026][jgarciad8][Controlador de autenticación]