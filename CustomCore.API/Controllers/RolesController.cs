//[INICIO][16/9/2026][jgarciad8][Controlador protegido de roles]
using System.IdentityModel.Tokens.Jwt;
using CustomCore.API.Dtos;
using CustomCore.API.Seguridad;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RolesController(RolService servicio) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermisosSistema.RolesLeer)]
    public Task<List<RolDto>> Obtener(CancellationToken ct)
        => servicio.ObtenerAsync(ct);

    [HttpGet("{id:int:min(1)}")]
    [Authorize(Policy = PermisosSistema.RolesLeer)]
    public async Task<ActionResult<RolDto>> ObtenerPorId(
        int id,
        CancellationToken ct)
    {
        var rol = await servicio.ObtenerPorIdAsync(id, ct);

        if (rol is null)
            return NoEncontrado();

        return Ok(rol);
    }

    [HttpPost]
    [Authorize(Policy = PermisosSistema.RolesCrear)]
    [ProducesResponseType(typeof(RolDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<RolDto>> Crear(
        GuardarRolRequest request,
        CancellationToken ct)
    {
        var rol = await servicio.CrearAsync(request, ct);

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = rol.IdRol },
            rol);
    }

    [HttpPut("{id:int:min(1)}")]
    [Authorize(Policy = PermisosSistema.RolesEditar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Actualizar(
        int id,
        GuardarRolRequest request,
        CancellationToken ct)
    {
        return await servicio.ActualizarAsync(id, request, ct)
            ? NoContent()
            : NoEncontrado();
    }

    [HttpDelete("{id:int:min(1)}")]
    [Authorize(Policy = PermisosSistema.RolesEliminar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        return await servicio.EliminarAsync(id, ct)
            ? NoContent()
            : NoEncontrado();
    }

    [HttpPut("{id:int:min(1)}/usuarios/{usuarioId:int:min(1)}")]
    [Authorize(Policy = PermisosSistema.UsuariosAsignarRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> AsignarUsuario(
        int id,
        int usuarioId,
        CancellationToken ct)
        => CambiarAsignacion(id, usuarioId, true, ct);

    [HttpDelete("{id:int:min(1)}/usuarios/{usuarioId:int:min(1)}")]
    [Authorize(Policy = PermisosSistema.UsuariosAsignarRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> RetirarUsuario(
        int id,
        int usuarioId,
        CancellationToken ct)
        => CambiarAsignacion(id, usuarioId, false, ct);


    private async Task<IActionResult> CambiarAsignacion(
        int rolId,
        int usuarioId,
        bool asignar,
        CancellationToken ct)
    {
        if (!int.TryParse(
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            out var usuarioActualId)
            || usuarioActualId <= 0)
        {
            return Unauthorized();
        }

        var resultado = await servicio.CambiarAsignacionAsync(
            rolId, usuarioId, usuarioActualId, asignar, ct);

        return resultado ? NoContent() : NoEncontrado();
    }

    private ObjectResult NoEncontrado()
        => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Registro no encontrado",
            detail: "El rol o el usuario solicitado no existe.");

    //[INICIO][16/9/2026][jgarciad8][Endpoints de permisos asociados a roles]
    [HttpGet("{id:int:min(1)}/permisos")]
    [Authorize(Policy = PermisosSistema.RolesLeer)]
    public async Task<ActionResult<List<PermisoDto>>> ObtenerPermisos(
        int id, CancellationToken ct)
    {
        var permisos = await servicio.ObtenerPermisosAsync(id, ct);

        if (permisos is null)
            return NoEncontrado();

        return Ok(permisos);
    }

    [HttpPut("{id:int:min(1)}/permisos/{permisoId:int:min(1)}")]
    [Authorize(Policy = PermisosSistema.RolesAsignarPermisos)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> AsignarPermiso(
        int id, int permisoId, CancellationToken ct)
        => CambiarPermiso(id, permisoId, true, ct);

    [HttpDelete("{id:int:min(1)}/permisos/{permisoId:int:min(1)}")]
    [Authorize(Policy = PermisosSistema.RolesAsignarPermisos)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> RetirarPermiso(
        int id, int permisoId, CancellationToken ct)
        => CambiarPermiso(id, permisoId, false, ct);

    private async Task<IActionResult> CambiarPermiso(
        int rolId, int permisoId, bool asignar, CancellationToken ct)
    {
        if (!int.TryParse(
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            out var usuarioActualId)
            || usuarioActualId <= 0)
        {
            return Unauthorized();
        }

        var resultado = await servicio.CambiarPermisoAsync(
            rolId, permisoId, usuarioActualId, asignar, ct);

        if (!resultado)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Registro no encontrado",
                detail: "El rol o el permiso solicitado no existe.");
        }

        return NoContent();
    }
    //[FIN][16/9/2026][jgarciad8][Endpoints de permisos asociados a roles]
}
//[FIN][16/9/2026][jgarciad8][Controlador protegido de roles]