using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/pagos")]
    public sealed class PagosController(PagoStripeService servicio) : ControllerBase
    {
        //[INICIO][16/9/2026][Rodriale][Genera el enlace de pago de una factura; se puede mandar al cliente o abrir en caja]
        [HttpPost("facturas/{facturaId:int}/sesion")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<SesionPagoDto>> CrearSesion(
            int facturaId,
            CancellationToken cancellationToken)
            {
            var sesion = await servicio.CrearSesionAsync(facturaId, cancellationToken);
            return sesion is null ? NotFound() : Ok(sesion);
        }
        //[FIN][16/9/2026][Rodriale][Genera el enlace de pago de una factura se puede mandar al cliente o abrir en caja]
    }
}
//[FIN][16/9/2026][Rodriale][Controlador de pagos con Stripe]