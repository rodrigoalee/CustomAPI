using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Mvc;
//[INICIO][17/9/2026][jgarciad8][Dependencias de autorización]
using CustomCore.API.Seguridad;
using Microsoft.AspNetCore.Authorization;
//[FIN][17/9/2026][jgarciad8][Dependencias de autorización]

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/vehiculos")]
    [Authorize]
    public sealed class VehiculosController(VehiculoService servicio) : ControllerBase
    {

        [HttpGet]
        [Authorize(Policy = PermisosSistema.VehiculosLeer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<ResultadoPaginado<VehiculoListaDto>> Obtener(
            [FromQuery] ConsultaVehiculos consulta,
            CancellationToken cancellationToken) =>
            servicio.ObtenerAsync(consulta, cancellationToken);

        [HttpGet("{id:int}")]
        [Authorize(Policy = PermisosSistema.VehiculosLeer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VehiculoDetalleDto>> ObtenerPorId(
            int id,
            CancellationToken cancellationToken)
        {
            var vehiculo = await servicio.ObtenerPorIdAsync(id, cancellationToken);
            return vehiculo is null ? NotFound() : Ok(vehiculo);
        }

        [HttpPost]
        [Authorize(Policy = PermisosSistema.VehiculosCrear)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Crear(
            CrearVehiculosRequest request,
            CancellationToken cancellationToken)
        {
            var id = await servicio.CrearAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ObtenerPorId), new { id }, null);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = PermisosSistema.VehiculosEditar)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Actualizar(
            int id,
            ActualizarVehiculoRequest request,
            CancellationToken cancellationToken)
        {
            var actualizado = await servicio.ActualizarAsync(id, request, cancellationToken);
            return actualizado ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = PermisosSistema.VehiculosEliminar)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Eliminar(int id, CancellationToken cancellationToken)
        {
            var eliminado = await servicio.EliminarAsync(id, cancellationToken);
            return eliminado ? NoContent() : NotFound();
        }
    }
}