//[INICIO][28/8/2026][Rodriale][Controlador liviano que solo traduce HTTP hacia el servicio]
using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/clientes")]
    public sealed class ClientesController(ClienteService servicio) : ControllerBase
    {
        
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<ResultadoPaginado<ClienteResumenDto>> Obtener(
            [FromQuery] ConsultaPaginada consulta,
            CancellationToken cancellationToken) =>
            servicio.ObtenerAsync(consulta, cancellationToken);

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClienteDetalleDto>> ObtenerPorId(
         int id,
         CancellationToken cancellationToken)
        {
            var cliente = await servicio.ObtenerPorIdAsync(id, cancellationToken);
            return cliente is null ? NotFound() : Ok(cliente);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Crear(
        CrearClienteRequest request,
        CancellationToken cancellationToken)
        {
            var id = await servicio.CrearAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ObtenerPorId), new { id }, null);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Actualizar(
       int id,
       ActualizarClienteRequest request,
       CancellationToken cancellationToken)
        {
            var actualizado = await servicio.ActualizarAsync(id, request, cancellationToken);
            return actualizado ? NoContent() : NotFound();
        }
    }
}
//[FIN][28/8/2026][Rodriale][Controlador liviano que solo traduce HTTP hacia el servicio]