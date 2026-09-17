//[INICIO][16/9/2026][Rodriale][Controlador de pagos en línea con Stripe]
using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CustomCore.API.Seguridad;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/pagos")]

    [Authorize]
    public sealed class PagosController(PagoStripeService servicio) : ControllerBase
    {
        //[INICIO][16/9/2026][Rodriale][Genera el enlace de pago de una factura; se puede mandar al cliente o abrir en caja]
        [HttpPost("facturas/{facturaId:int}/sesion")]
        [Authorize(Policy = PermisosSistema.PagosCrearSesion)]
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
        //[FIN][16/9/2026][Rodriale][Genera el enlace de pago de una factura]

        //[INICIO][16/9/2026][Rodriale][Cobro con interfaz propia: recibe el pm_ de Stripe Elements, o pm_card_visa en pruebas desde Postman]
        [HttpPost("facturas/{facturaId:int}/cobro")]
        [Authorize(Policy = PermisosSistema.PagosCobrar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<ResultadoCobroDto>> Cobrar(
            int facturaId,
            CobrarFacturaRequest request,
            CancellationToken cancellationToken)
        {
            var resultado = await servicio.CobrarAsync(facturaId, request, cancellationToken);
            return resultado is null ? NotFound() : Ok(resultado);
        }
        //[FIN][16/9/2026][Rodriale][Cobro con interfaz propia]

        //[INICIO][16/9/2026][Rodriale][Stripe llama aquí al confirmar un pago. AllowAnonymous porque Stripe no tiene JWT: cuando la Fase 6 cierre la API, este endpoint debe seguir abierto y su seguridad es la firma]
        [AllowAnonymous]
        [HttpPost("webhook")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Webhook(CancellationToken cancellationToken)
        {
            //[INICIO][16/9/2026][Rodriale][Se lee el cuerpo crudo: la firma se calcula sobre los bytes exactos, cualquier reformateo la invalida]
            using var lector = new StreamReader(Request.Body);
            var cuerpo = await lector.ReadToEndAsync(cancellationToken);
            var firma = Request.Headers["Stripe-Signature"].ToString();
            //[FIN][16/9/2026][Rodriale][Se lee el cuerpo crudo]

            var resultado = await servicio.ProcesarWebhookAsync(cuerpo, firma, cancellationToken);

            return resultado == ResultadoWebhook.FirmaInvalida ? BadRequest() : Ok();
        }
        //[FIN][16/9/2026][Rodriale][Stripe llama aquí al confirmar un pago]
    }
}
//[FIN][16/9/2026][Rodriale][Controlador de pagos en línea con Stripe]