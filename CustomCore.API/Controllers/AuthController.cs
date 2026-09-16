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
}
//[FIN][16/9/2026][jgarciad8][Controlador de autenticación]