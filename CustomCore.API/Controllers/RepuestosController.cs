using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/repuestos")]
    public sealed class RepuestosController(RepuestoService servicio) : ControllerBase
    {
        //[INICIO][31/8/2026][Rodriale][Obtener listado con paginación y filtros]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<ResultadoPaginado<RepuestoDto>> Obtener(
            [FromQuery] ConsultaCatalogo consulta,
            CancellationToken cancellationToken) =>
            servicio.ObtenerAsync(consulta, cancellationToken);
        //[FIN][31/8/2026][Rodriale][Obtener listado con paginación y filtros]

        //[INICIO][31/8/2026][Rodriale][Detalle con datos del dueño y conteos de historial en una sola consulta]
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RepuestoDto>> ObtenerPorId(
            int id,
            CancellationToken cancellationToken)
        {
            var repuesto = await servicio.ObtenerPorIdAsync(id, cancellationToken);
            return repuesto is null ? NotFound() : Ok(repuesto);
        }
        //[FIN][31/8/2026][Rodriale][Detalle con datos del dueño y conteos de historial en una sola consulta]

        //[INICIO][31/8/2026][Rodriale][Crear y actualizar con validaciones de negocio]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Crear(
            CrearRepuestoRequest request,
            CancellationToken cancellationToken)
        {
            var id = await servicio.CrearAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ObtenerPorId), new { id }, null);
        }
        //[FIN][31/8/2026][Rodriale][Crear y actualizar con validaciones de negocio]

        //[INICIO][31/8/2026][Rodriale][Actualizar con validaciones de negocio]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Actualizar(
            int id,
            ActualizarRepuestoRequest request,
            CancellationToken cancellationToken)
        {
            var actualizado = await servicio.ActualizarAsync(id, request, cancellationToken);
            return actualizado ? NoContent() : NotFound();
        }
        //[FIN][31/8/2026][Rodriale][Actualizar con validaciones de negocio]

        //[INICIO][31/8/2026][Rodriale][DELETE hace baja lógica, no borrado físico]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DarDeBaja(int id, CancellationToken cancellationToken)
        {
            var dadoDeBaja = await servicio.DarDeBajaAsync(id, cancellationToken);
            return dadoDeBaja ? NoContent() : NotFound();
        }
        //[FIN][31/8/2026][Rodriale][DELETE hace baja lógica, no borrado físico]
    }
}