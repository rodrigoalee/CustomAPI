//[INICIO][31/8/2026][Rodriale][Controlador de facturación: emitir, consultar y registrar el cobro]
using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/facturas")]
    public sealed class FacturasController(FacturaService servicio) : ControllerBase
    {
        //[INICIO][31/8/2026][Rodriale][Historial de facturas con filtros y paginación]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<ResultadoPaginado<FacturaListaDto>> Obtener(
            [FromQuery] ConsultaFacturas consulta,
            CancellationToken cancellationToken) =>
            servicio.ObtenerAsync(consulta, cancellationToken);
        //[FIN][31/8/2026][Rodriale][Historial de facturas]

        //[INICIO][31/8/2026][Rodriale][Factura completa con sus renglones]
        [HttpGet("{id:int}")]
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
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Crear(
            CrearFacturaRequest request,
            CancellationToken cancellationToken)
        {
            var id = await servicio.CrearAsync(request, cancellationToken);
            return id is null
                ? NotFound()
                : CreatedAtAction(nameof(ObtenerPorId), new { id }, null);
        }
        //[FIN][31/8/2026][Rodriale][Emitir factura]

        //[INICIO][31/8/2026][Rodriale][Marcar la factura como pagada; en la Fase 8 esto lo disparará Stripe]
        [HttpPatch("{id:int}/pago")]
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
