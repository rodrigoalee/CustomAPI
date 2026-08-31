//[INICIO][31/8/2026][Rodriale][Controlador de órdenes de trabajo: solo traduce HTTP, toda la lógica vive en el servicio]
using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/ordenes")]
    public sealed class OrdenesTrabajoController(OrdenTrabajoService servicio) : ControllerBase
    {
        //[INICIO][31/8/2026][Rodriale][Tablero de órdenes con filtros y paginación]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<ResultadoPaginado<OrdenListaDto>> Obtener(
            [FromQuery] ConsultaOrdenes consulta,
            CancellationToken cancellationToken) =>
            servicio.ObtenerAsync(consulta, cancellationToken);
        //[FIN][31/8/2026][Rodriale][Tablero de órdenes]

        //[INICIO][31/8/2026][Rodriale][Detalle de una orden; si no existe se responde 404 y no un 200 con nulos]
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrdenDetalleDto>> ObtenerPorId(
            int id,
            CancellationToken cancellationToken)
        {
            var orden = await servicio.ObtenerPorIdAsync(id, cancellationToken);
            return orden is null ? NotFound() : Ok(orden);
        }
        //[FIN][31/8/2026][Rodriale][Detalle de una orden]

        //[INICIO][31/8/2026][Rodriale][Alta de la orden; el 409 sale cuando falta stock o el catálogo no cuadra, y lo arma el manejador global]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Crear(
            CrearOrdenRequest request,
            CancellationToken cancellationToken)
        {
            var id = await servicio.CrearAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ObtenerPorId), new { id }, null);
        }
        //[FIN][31/8/2026][Rodriale][Alta de la orden]

        //[INICIO][31/8/2026][Rodriale][Actualizar diagnóstico y mecánico asignado]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Actualizar(
            int id,
            ActualizarOrdenRequest request,
            CancellationToken cancellationToken)
        {
            var actualizada = await servicio.ActualizarAsync(id, request, cancellationToken);
            return actualizada ? NoContent() : NotFound();
        }
        //[FIN][31/8/2026][Rodriale][Actualizar diagnóstico y mecánico asignado]

        //[INICIO][31/8/2026][Rodriale][Mover la orden de estado; finalizar sella la fecha de salida]
        [HttpPatch("{id:int}/estado")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> CambiarEstado(
            int id,
            CambiarEstadoOrdenRequest request,
            CancellationToken cancellationToken)
        {
            var actualizada = await servicio.CambiarEstadoAsync(id, request, cancellationToken);
            return actualizada ? NoContent() : NotFound();
        }
        //[FIN][31/8/2026][Rodriale][Mover la orden de estado]

        //[INICIO][31/8/2026][Rodriale][Agregar trabajo a una orden abierta; descuenta stock y recalcula el total]
        [HttpPost("{id:int}/lineas")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> AgregarLinea(
            int id,
            CrearLineaOrdenRequest request,
            CancellationToken cancellationToken)
        {
            var idLinea = await servicio.AgregarLineaAsync(id, request, cancellationToken);
            return idLinea is null
                ? NotFound()
                : CreatedAtAction(nameof(ObtenerPorId), new { id }, null);
        }
        //[FIN][31/8/2026][Rodriale][Agregar trabajo a una orden abierta]

        //[INICIO][31/8/2026][Rodriale][Quitar una línea; si era repuesto, regresa a bodega]
        [HttpDelete("{id:int}/lineas/{lineaId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> QuitarLinea(
            int id,
            int lineaId,
            CancellationToken cancellationToken)
        {
            var eliminada = await servicio.QuitarLineaAsync(id, lineaId, cancellationToken);
            return eliminada ? NoContent() : NotFound();
        }
        //[FIN][31/8/2026][Rodriale][Quitar una línea]
    }
}
//[FIN][31/8/2026][Rodriale][Controlador de órdenes de trabajo]
