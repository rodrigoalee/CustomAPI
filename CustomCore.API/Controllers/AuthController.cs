//[INICIO][16/9/2026][jgarciad8][Controlador de autenticación]
using System.IdentityModel.Tokens.Jwt;
using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CustomCore.API.Controllers;

[ApiController]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(
    AuthService servicio,
    RegistroService registroService,
    CuentaGoogleService cuentaGoogleService) : ControllerBase
{
    //[INICIO][23/10/2026][Rodriale][Login con Google. Sin la política "registro" a propósito: reparte por IP y todas las llamadas llegan desde el servidor de Vercel, así que los clientes legítimos se limitarían entre sí. No agregarla por simetría]
    [AllowAnonymous]
    [HttpPost("google")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> IniciarSesionConGoogle(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var respuesta = await cuentaGoogleService.IniciarSesionAsync(
            request,
            cancellationToken);

        if (respuesta is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "No se pudo iniciar sesión con Google",
                detail: "No pudimos verificar tu cuenta de Google. Intenta de nuevo.");
        }

        return Ok(respuesta);
    }
    //[FIN][23/10/2026][Rodriale][Login con Google]

    //[INICIO][23/10/2026][Rodriale][Completar el perfil tras el alta por Google: el teléfono crea la ficha del taller]
    [Authorize]
    [HttpPost("perfil")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompletarPerfil(
        CompletarPerfilRequest request,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            out var idUsuario)
            || idUsuario <= 0)
        {
            return Unauthorized();
        }

        await cuentaGoogleService.CompletarPerfilAsync(
            idUsuario, request, cancellationToken);

        return NoContent();
    }
    //[FIN][23/10/2026][Rodriale][Completar el perfil tras el alta por Google]

    //[INICIO][14/10/2026][Rodriale][Alta pública del portal: anónima por necesidad, con techo de intentos por IP]
    [AllowAnonymous]
    [EnableRateLimiting("registro")]
    [HttpPost("registro")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Registrar(
        RegistroRequest request,
        CancellationToken cancellationToken)
    {
        //[INICIO][14/10/2026][Rodriale][200 y no 201: lo que devolvemos es la sesión, no un recurso que el cliente pueda consultar por URL]
        return Ok(await registroService.RegistrarAsync(request, cancellationToken));
        //[FIN][14/10/2026][Rodriale][200 y no 201]
    }
    //[FIN][14/10/2026][Rodriale][Alta pública del portal]

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