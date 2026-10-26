//[INICIO][31/8/2026][Rodriale][DTOs del módulo de facturación]
using System.ComponentModel.DataAnnotations;
using CustomCore.API.Entidades;

namespace CustomCore.API.Dtos
{
    //[INICIO][31/8/2026][Rodriale][Filtros del historial de cobros]
    public record ConsultaFacturas : ConsultaPaginada
    {
        [Range(1, int.MaxValue)]
        public int? ClienteId { get; init; }

        public EstadoPago? EstadoPago { get; init; }
        public DateTimeOffset? Desde { get; init; }
        public DateTimeOffset? Hasta { get; init; }

        //[INICIO][12/10/2026][Rodriale][Filtro por sede de la orden; solo lo respeta quien ve todas]
        [Range(1, int.MaxValue)]
        public int? SucursalId { get; init; }
        //[FIN][12/10/2026][Rodriale][Filtro por sede de la orden]

        //[INICIO][26/10/2026][Rodriale][La factura de una orden, para la sección de cobro del detalle]
        [Range(1, int.MaxValue)]
        public int? OrdenTrabajoId { get; init; }
        //[FIN][26/10/2026][Rodriale][La factura de una orden]
    }
    //[FIN][31/8/2026][Rodriale][Filtros del historial de cobros]

    //[INICIO][31/8/2026][Rodriale][Fila del listado de facturas]
    public record FacturaListaDto(
        int IdFacturaEncabezado,
        int SucursalId,
        int OrdenTrabajoId,
        DateTimeOffset FechaEmision,
        string NombreFacturacion,
        string? NitFacturacion,
        decimal TotalPagado,
        EstadoPago EstadoPago);
    //[FIN][31/8/2026][Rodriale][Fila del listado de facturas]

    //[INICIO][31/8/2026][Rodriale][La factura completa, tal como se imprime o se manda por correo]
    public record FacturaDetalleDto(
        int IdFacturaEncabezado,
        int SucursalId,
        int OrdenTrabajoId,
        DateTimeOffset FechaEmision,
        int ClienteId,
        string NombreFacturacion,
        string? NitFacturacion,
        string Placa,
        decimal Subtotal,
        decimal TasaImpuesto,
        decimal MontoImpuesto,
        decimal TotalPagado,
        EstadoPago EstadoPago,
        string? StripePaymentId,
        string UsuarioCajero,
        IReadOnlyList<LineaFacturaDto> Lineas);
    //[FIN][31/8/2026][Rodriale][La factura completa]

    //[INICIO][31/8/2026][Rodriale][Renglón de la factura; la descripción es texto congelado, no un id: si el repuesto se renombra, la factura vieja no cambia]
    public record LineaFacturaDto(
        int IdFacturaDetalle,
        string DescripcionItem,
        int Cantidad,
        decimal PrecioUnitario,
        decimal Subtotal);
    //[FIN][31/8/2026][Rodriale][Renglón de la factura]

    //[INICIO][31/8/2026][Rodriale][Para facturar solo hace falta la orden y quién cobra: todo lo demás se copia de la orden]
    public record CrearFacturaRequest(
        [Range(1, int.MaxValue)] int OrdenTrabajoId,
        //[INICIO][26/10/2026][Rodriale][Lo pone el controlador desde el JWT, como en las órdenes; el cliente no necesita mandarlo]
        int UsuarioCajeroId = 0);
        //[FIN][26/10/2026][Rodriale][Lo pone el controlador desde el JWT]
    //[FIN][31/8/2026][Rodriale][Para facturar solo hace falta la orden y quién cobra]

    //[INICIO][31/8/2026][Rodriale][Marcar el cobro; el StripePaymentId llega de verdad en la Fase 8, por ahora es la referencia del voucher]
    public record RegistrarPagoRequest(
        [MaxLength(100)] string? StripePaymentId);
    //[FIN][31/8/2026][Rodriale][Marcar el cobro]
}
//[FIN][31/8/2026][Rodriale][DTOs del módulo de facturación]
