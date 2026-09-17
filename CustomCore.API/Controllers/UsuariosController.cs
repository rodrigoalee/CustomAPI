//[INICIO][16/9/2026][jgarciad8][Controlador del módulo de usuarios]
using CustomCore.API.Dtos;
using CustomCore.API.Seguridad;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace CustomCore.API.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UsuariosController(UsuarioService servicio)
    : ControllerBase
{
    //[INICIO][16/9/2026][jgarciad8][Consulta de usuarios protegida por permiso]
    [HttpGet]
    [Authorize(Policy = PermisosSistema.UsuariosLeer)]
    [ProducesResponseType(
        typeof(ResultadoPaginado<UsuarioResumenDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<ResultadoPaginado<UsuarioResumenDto>> Obtener(
        [FromQuery] ConsultaPaginada consulta,
        CancellationToken cancellationToken)
    {
        return servicio.ObtenerAsync(consulta, cancellationToken);
    }
    //[FIN][16/9/2026][jgarciad8][Consulta de usuarios protegida por permiso]

    //[INICIO][16/9/2026][jgarciad8][Consulta protegida de un usuario por identificador]
    [HttpGet("{id:int:min(1)}")]
    [Authorize(Policy = PermisosSistema.UsuariosLeer)]
    [ProducesResponseType(typeof(UsuarioResumenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioResumenDto>> ObtenerPorId(
        int id,
        CancellationToken cancellationToken)
    {
        var usuario = await servicio.ObtenerPorIdAsync(
            id,
            cancellationToken);

        if (usuario is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Usuario no encontrado",
                detail: "No existe un usuario con el identificador indicado.");
        }

        return Ok(usuario);
    }
    //[FIN][16/9/2026][jgarciad8][Consulta protegida de un usuario por identificador]

    //[INICIO][16/9/2026][jgarciad8][Endpoint protegido para crear usuarios]
    [HttpPost]
    [Authorize(Policy = PermisosSistema.UsuariosCrear)]
    [ProducesResponseType(typeof(UsuarioResumenDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResumenDto>> Crear(
        CrearUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await servicio.CrearAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = usuario.IdUsuario },
            usuario);
    }
    //[FIN][16/9/2026][jgarciad8][Endpoint protegido para crear usuarios]

    //[INICIO][16/9/2026][jgarciad8][Endpoint protegido para editar usuarios]
    [HttpPut("{id:int:min(1)}")]
    [Authorize(Policy = PermisosSistema.UsuariosEditar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        int id,
        ActualizarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var actualizado = await servicio.ActualizarAsync(
            id,
            request,
            cancellationToken);

        if (!actualizado)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Usuario no encontrado",
                detail: "No existe un usuario con el identificador indicado.");
        }

        return NoContent();
    }
    //[FIN][16/9/2026][jgarciad8][Endpoint protegido para editar usuarios]

    //[INICIO][16/9/2026][jgarciad8][Endpoint protegido para cambiar el estado de una cuenta]
    [HttpPatch("{id:int:min(1)}/estado")]
    [Authorize(Policy = PermisosSistema.UsuariosCambiarEstado)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarEstado(
        int id,
        CambiarEstadoUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        
        if (!int.TryParse(
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            out var usuarioActualId)
            || usuarioActualId <= 0)
        {
            return Unauthorized();
        }

        
        var actualizado = await servicio.CambiarEstadoAsync(
            id,
            request.Activo!.Value,
            usuarioActualId,
            cancellationToken);

        if (!actualizado)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Usuario no encontrado",
                detail: "No existe un usuario con el identificador indicado.");
        }

        return NoContent();
    }
    //[FIN][16/9/2026][jgarciad8][Endpoint protegido para cambiar el estado de una cuenta]

}
//[FIN][16/9/2026][jgarciad8][Controlador del módulo de usuarios]