using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/citas")]
    public sealed class CitasController(CitaService servicio) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<ResultadoPaginado<CitaListaDto>> Obtener(
            [FromQuery] ConsultaCitas consulta,
            CancellationToken cancellationToken) =>
            servicio.ObtenerAsync(consulta, cancellationToken);

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CitaDetalleDto>> ObtenerPorId(
            int id,
            CancellationToken cancellationToken)
        {
            var cita = await servicio.ObtenerPorIdAsync(id, cancellationToken);
            return cita is null ? NotFound() : Ok(cita);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Crear(
            CrearCitaRequest request,
            CancellationToken cancellationToken)
        {
            var id = await servicio.CrearAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ObtenerPorId), new { id }, null);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Reprogramar(
            int id,
            ReprogramarCitaRequest request,
            CancellationToken cancellationToken)
        {
            var reprogramada = await servicio.ReprogramarAsync(id, request, cancellationToken);
            return reprogramada ? NoContent() : NotFound();
        }

        //[INICIO][31/8/2026][Rodriale][Confirmar o cancelar sin tocar el resto de la cita]
        [HttpPatch("{id:int}/estado")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> CambiarEstado(
            int id,
            CambiarEstadoCitaRequest request,
            CancellationToken cancellationToken)
        {
            var actualizada = await servicio.CambiarEstadoAsync(id, request, cancellationToken);
            return actualizada ? NoContent() : NotFound();
        }
        //[FIN][31/8/2026][Rodriale][Confirmar o cancelar sin tocar el resto de la cita]
    }
}
