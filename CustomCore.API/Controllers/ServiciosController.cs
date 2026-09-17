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
    [Route("api/servicios")]
    [Authorize]
    public sealed class ServiciosController(ServicioService servicio) : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = PermisosSistema.ServiciosLeer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<ResultadoPaginado<ServicioDto>> Obtener(
            [FromQuery] ConsultaCatalogo consulta,
            CancellationToken cancellationToken) =>
            servicio.ObtenerAsync(consulta, cancellationToken);

        [HttpGet("{id:int}")]
        [Authorize(Policy = PermisosSistema.ServiciosLeer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ServicioDto>> ObtenerPorId(
            int id,
            CancellationToken cancellationToken)
        {
            var resultado = await servicio.ObtenerPorIdAsync(id, cancellationToken);
            return resultado is null ? NotFound() : Ok(resultado);
        }

        [HttpPost]
        [Authorize(Policy = PermisosSistema.ServiciosCrear)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Crear(
            CrearServicioRequest request,
            CancellationToken cancellationToken)
        {
            var id = await servicio.CrearAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ObtenerPorId), new { id }, null);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = PermisosSistema.ServiciosEditar)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Actualizar(
            int id,
            ActualizarServicioRequest request,
            CancellationToken cancellationToken)
        {
            var actualizado = await servicio.ActualizarAsync(id, request, cancellationToken);
            return actualizado ? NoContent() : NotFound();
        }

        //[INICIO][31/8/2026][Rodriale][DELETE hace baja lógica, no borrado físico]
        [HttpDelete("{id:int}")]
        [Authorize(Policy = PermisosSistema.ServiciosBaja)]
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