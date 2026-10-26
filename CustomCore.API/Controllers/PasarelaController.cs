//[INICIO][26/10/2026][Rodriale][Pasarela pública: la abre el cliente desde WhatsApp sin cuenta; la seguridad es el enlace firmado]
using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/pasarela/{token}")]
    [AllowAnonymous]
    [EnableRateLimiting("pasarela")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public sealed class PasarelaController(PasarelaPruebaService servicio) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResumenPagoDto>> ObtenerResumen(string token, CancellationToken cancellationToken)
        {
            var resumen = await servicio.ObtenerResumenAsync(token, cancellationToken);
            return resumen is null ? NotFound() : Ok(resumen);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResultadoPagoDto>> Pagar(
            string token,
            PagarConTarjetaRequest request,
            CancellationToken cancellationToken)
        {
            var resultado = await servicio.PagarAsync(token, request, cancellationToken);
            return resultado is null ? NotFound() : Ok(resultado);
        }
    }
}
//[FIN][26/10/2026][Rodriale][Pasarela pública]
