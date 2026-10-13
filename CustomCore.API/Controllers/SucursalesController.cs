//[INICIO][17/9/2026][Rodriale][Controlador delgado del catálogo de sucursales]
using CustomCore.API.Dtos;
using CustomCore.API.Seguridad;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/sucursales")]
    //[INICIO][12/10/2026][Rodriale][Antes cualquiera podía crear o cerrar sedes sin iniciar sesión]
    [Authorize]
    //[FIN][12/10/2026][Rodriale][Antes cualquiera podía crear o cerrar sedes sin iniciar sesión]
    public sealed class SucursalesController(SucursalService servicio) : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = PermisosSistema.SucursalesLeer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<ResultadoPaginado<SucursalDto>> Obtener(
            [FromQuery] ConsultaCatalogo consulta,
            CancellationToken cancellationToken) =>
            servicio.ObtenerAsync(consulta, cancellationToken);

        [HttpGet("{id:int}")]
        [Authorize(Policy = PermisosSistema.SucursalesLeer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SucursalDto>> ObtenerPorId(int id, CancellationToken cancellationToken)
        {
            var sucursal = await servicio.ObtenerPorIdAsync(id, cancellationToken);
            return sucursal is null ? NotFound() : Ok(sucursal);
        }

        [HttpPost]
        [Authorize(Policy = PermisosSistema.SucursalesGestionar)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Crear(CrearSucursalRequest request, CancellationToken cancellationToken)
        {
            var id = await servicio.CrearAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ObtenerPorId), new { id }, null);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = PermisosSistema.SucursalesGestionar)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Actualizar(
            int id,
            ActualizarSucursalRequest request,
            CancellationToken cancellationToken)
        {
            var actualizada = await servicio.ActualizarAsync(id, request, cancellationToken);
            return actualizada ? NoContent() : NotFound();
        }

        //[INICIO][17/9/2026][Rodriale][DELETE hace baja lógica, no borrado físico]
        [HttpDelete("{id:int}")]
        [Authorize(Policy = PermisosSistema.SucursalesGestionar)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DarDeBaja(int id, CancellationToken cancellationToken)
        {
            var dadaDeBaja = await servicio.DarDeBajaAsync(id, cancellationToken);
            return dadaDeBaja ? NoContent() : NotFound();
        }
        //[FIN][17/9/2026][Rodriale][DELETE hace baja lógica]
    }
}
//[FIN][17/9/2026][Rodriale][Controlador del catálogo de sucursales]