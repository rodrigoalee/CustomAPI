//[INICIO][12/10/2026][Rodriale][Controlador delgado del inventario por sucursal]
using CustomCore.API.Dtos;
using CustomCore.API.Seguridad;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/sucursales/{sucursalId:int}/inventario")]
    [Authorize]
    public sealed class InventarioController(InventarioService servicio) : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = PermisosSistema.InventarioLeer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultadoPaginado<InventarioDto>>> Obtener(
            int sucursalId,
            [FromQuery] ConsultaPaginada consulta,
            CancellationToken cancellationToken)
        {
            var inventario = await servicio.ObtenerAsync(sucursalId, consulta, cancellationToken);
            return inventario is null ? NotFound() : Ok(inventario);
        }

        [HttpPut("{repuestoId:int}")]
        [Authorize(Policy = PermisosSistema.InventarioAjustar)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Fijar(
            int sucursalId,
            int repuestoId,
            FijarInventarioRequest request,
            CancellationToken cancellationToken)
        {
            var fijado = await servicio.FijarAsync(sucursalId, repuestoId, request.Cantidad, cancellationToken);
            return fijado ? NoContent() : NotFound();
        }
    }
}
//[FIN][12/10/2026][Rodriale][Controlador delgado del inventario por sucursal]
