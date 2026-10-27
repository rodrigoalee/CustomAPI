//[INICIO][26/10/2026][Rodriale][Cotizaciones de una orden: el taller las consulta y las envía al cliente]
using System.IdentityModel.Tokens.Jwt;
using CustomCore.API.Dtos;
using CustomCore.API.Seguridad;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/ordenes/{ordenId:int}/cotizaciones")]
    [Authorize]
    public sealed class CotizacionesController(CotizacionService servicio) : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = PermisosSistema.OrdenesLeer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<IReadOnlyList<CotizacionDto>> Obtener(int ordenId, CancellationToken cancellationToken) =>
            servicio.ObtenerDeOrdenAsync(ordenId, cancellationToken);

        [HttpPost]
        [Authorize(Policy = PermisosSistema.OrdenesCotizar)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Crear(
            int ordenId,
            CrearCotizacionRequest request,
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var usuarioId) || usuarioId <= 0)
                return Unauthorized();

            var id = await servicio.CrearAsync(ordenId, request, usuarioId, cancellationToken);
            return id is null ? NotFound() : Created($"/api/ordenes/{ordenId}/cotizaciones", new { idCotizacion = id });
        }
    }
}
//[FIN][26/10/2026][Rodriale][Cotizaciones de una orden]
