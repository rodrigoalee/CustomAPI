//[INICIO][31/8/2026][Rodriale][Controlador de facturación: emitir, consultar y registrar el cobro]
using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using CustomCore.API.Seguridad;
using Microsoft.AspNetCore.Authorization;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/facturas")]

    //[INICIO][17/9/2026][jgarciad8][Autenticación obligatoria para facturación]
    [Authorize]
    //[FIN][17/9/2026][jgarciad8][Autenticación obligatoria para facturación]
    public sealed class FacturasController(FacturaService servicio) : ControllerBase
    {
        //[INICIO][31/8/2026][Rodriale][Historial de facturas con filtros y paginación]
        [HttpGet]
        [Authorize(Policy = PermisosSistema.FacturasLeer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<ResultadoPaginado<FacturaListaDto>> Obtener(
            [FromQuery] ConsultaFacturas consulta,
            CancellationToken cancellationToken) =>
            servicio.ObtenerAsync(consulta, cancellationToken);
        //[FIN][31/8/2026][Rodriale][Historial de facturas]

        //[INICIO][31/8/2026][Rodriale][Factura completa con sus renglones]
        [HttpGet("{id:int}")]
        [Authorize(Policy = PermisosSistema.FacturasLeer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FacturaDetalleDto>> ObtenerPorId(
            int id,
            CancellationToken cancellationToken)
        {
            var factura = await servicio.ObtenerPorIdAsync(id, cancellationToken);
            return factura is null ? NotFound() : Ok(factura);
        }
        //[FIN][31/8/2026][Rodriale][Factura completa con sus renglones]

        //[INICIO][31/8/2026][Rodriale][Emitir factura a partir de una orden finalizada; el 409 sale si ya estaba facturada o si el trabajo no ha terminado]
        [HttpPost]
        [Authorize(Policy = PermisosSistema.FacturasCrear)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Crear(
            CrearFacturaRequest request,
            CancellationToken cancellationToken)
        {
            //[INICIO][17/9/2026][jgarciad8][Identificación del cajero desde el JWT]
            if (!int.TryParse(
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
                out var usuarioActualId)
                || usuarioActualId <= 0)
            {
                return Unauthorized();
            }

            request = request with { UsuarioCajeroId = usuarioActualId };
            //[FIN][17/9/2026][jgarciad8][Identificación del cajero desde el JWT]


            var id = await servicio.CrearAsync(request, cancellationToken);
            return id is null
                ? NotFound()
                : CreatedAtAction(nameof(ObtenerPorId), new { id }, null);
        }
        //[FIN][31/8/2026][Rodriale][Emitir factura]

        //[INICIO][31/8/2026][Rodriale][Marcar la factura como pagada; en la Fase 8 esto lo disparará Stripe]
        [HttpPatch("{id:int}/pago")]
        [Authorize(Policy = PermisosSistema.FacturasRegistrarPagoManual)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> RegistrarPago(
            int id,
            RegistrarPagoRequest request,
            CancellationToken cancellationToken)
        {
            var registrado = await servicio.RegistrarPagoAsync(id, request, cancellationToken);
            return registrado ? NoContent() : NotFound();
        }
        //[FIN][31/8/2026][Rodriale][Marcar la factura como pagada]
    }
}
//[FIN][31/8/2026][Rodriale][Controlador de facturación]
