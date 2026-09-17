//[INICIO][16/9/2026][jgarciad8][Controlador protegido de permisos]
using CustomCore.API.Dtos;
using CustomCore.API.Seguridad;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers;

[ApiController]
[Route("api/permisos")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PermisosController(PermisoService servicio) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermisosSistema.PermisosLeer)]
    public Task<List<PermisoDto>> Obtener(CancellationToken ct)
        => servicio.ObtenerAsync(ct);

    [HttpGet("{id:int:min(1)}")]
    [Authorize(Policy = PermisosSistema.PermisosLeer)]
    public async Task<ActionResult<PermisoDto>> ObtenerPorId(
        int id, CancellationToken ct)
    {
        var permiso = await servicio.ObtenerPorIdAsync(id, ct);
        if (permiso is null)
            return NoEncontrado();

        return Ok(permiso);
    }

    [HttpPost]
    [Authorize(Policy = PermisosSistema.PermisosCrear)]
    [ProducesResponseType(typeof(PermisoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PermisoDto>> Crear(
        GuardarPermisoRequest request, CancellationToken ct)
    {
        var permiso = await servicio.CrearAsync(request, ct);

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = permiso.IdPermiso },
            permiso);
    }

    [HttpPut("{id:int:min(1)}")]
    [Authorize(Policy = PermisosSistema.PermisosEditar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Actualizar(
        int id, GuardarPermisoRequest request, CancellationToken ct)
        => await servicio.ActualizarAsync(id, request, ct)
            ? NoContent() : NoEncontrado();

    [HttpDelete("{id:int:min(1)}")]
    [Authorize(Policy = PermisosSistema.PermisosEliminar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
        => await servicio.EliminarAsync(id, ct)
            ? NoContent() : NoEncontrado();

    private ObjectResult NoEncontrado()
        => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Permiso no encontrado",
            detail: "No existe un permiso con el identificador indicado.");
}
//[FIN][16/9/2026][jgarciad8][Controlador protegido de permisos]