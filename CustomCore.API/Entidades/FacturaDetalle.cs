//[INICIO][28/8/2026][Rodriale][Detalle de factura con precio congelado al momento del cobro]
namespace CustomCore.API.Entidades;

public class FacturaDetalle
{
    public int IdFacturaDetalle { get; set; }
    public int FacturaId { get; set; }
    public required string DescripcionItem { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public FacturaEncabezado Factura { get; set; } = null!;
}
//[FIN][28/8/2026][Rodriale][Detalle de factura con precio congelado al momento del cobro]