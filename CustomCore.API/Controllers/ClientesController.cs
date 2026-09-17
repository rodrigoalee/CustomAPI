//[INICIO][28/8/2026][Rodriale][Controlador liviano que solo traduce HTTP hacia el servicio]
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
    [Route("api/clientes")]
    [Authorize]
    public sealed class ClientesController(ClienteService servicio) : ControllerBase
    {
       
        [HttpGet]

        //[INICIO][17/9/2026][jgarciad8][Permiso para consultar clientes]
        [Authorize(Policy = PermisosSistema.ClientesLeer)]
        //[FIN][17/9/2026][jgarciad8][Permiso para consultar clientes]

        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<ResultadoPaginado<ClienteResumenDto>> Obtener(
            [FromQuery] ConsultaPaginada consulta,
            CancellationToken cancellationToken) =>
            servicio.ObtenerAsync(consulta, cancellationToken);

        [HttpGet("{id:int}")]

        //[INICIO][17/9/2026][jgarciad8][Permiso para consultar clientes por id]
        [Authorize(Policy = PermisosSistema.ClientesLeer)]
        //[FIN][17/9/2026][jgarciad8][Permiso para consultar clientes por id]

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

        //[INICIO][17/9/2026][jgarciad8][Permiso para crear clientes]
        [Authorize(Policy = PermisosSistema.ClientesCrear)]
        //[FIN][17/9/2026][jgarciad8][Permiso para crear clientes]

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

        //[INICIO][17/9/2026][jgarciad8][Permiso para editar clientes]
        [Authorize(Policy = PermisosSistema.ClientesEditar)]
        //[FIN][17/9/2026][jgarciad8][Permiso para editar clientes]

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