//[INICIO][28/8/2026][Rodriale][Encabezado de factura con datos fiscales congelados y desglose de impuesto]
namespace CustomCore.API.Entidades;

public class FacturaEncabezado
{
    public int IdFacturaEncabezado { get; set; }
    public int OrdenTrabajoId { get; set; }
    public DateTimeOffset FechaEmision { get; set; }

    //[INICIO][28/8/2026][Rodriale][Snapshot fiscal: no se lee del cliente para no alterar facturas históricas]
    public int ClienteId { get; set; }
    public required string NombreFacturacion { get; set; }
    public string? NitFacturacion { get; set; }
    //[FIN][28/8/2026][Rodriale][Snapshot fiscal: no se lee del cliente para no alterar facturas históricas]

    //[INICIO][28/8/2026][Rodriale][Desglose para poder reconstruir el cobro con la tasa vigente al emitir]
    public decimal Subtotal { get; set; }
    public decimal TasaImpuesto { get; set; }
    public decimal MontoImpuesto { get; set; }
    public decimal TotalPagado { get; set; }
    //[FIN][28/8/2026][Rodriale][Desglose para poder reconstruir el cobro con la tasa vigente al emitir]

    public string? StripePaymentId { get; set; }
    public EstadoPago EstadoPago { get; set; } = EstadoPago.Pendiente;
    public int UsuarioCajeroId { get; set; }

    public OrdenTrabajoEncabezado OrdenTrabajo { get; set; } = null!;
    public Cliente Cliente { get; set; } = null!;
    public Usuario UsuarioCajero { get; set; } = null!;
    public ICollection<FacturaDetalle> Detalles { get; } = new List<FacturaDetalle>();
}
//[FIN][28/8/2026][Rodriale][Encabezado de factura con datos fiscales congelados y desglose de impuesto]