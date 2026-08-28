//[INICIO][28/8/2026][Rodriale][Detalle de orden: una línea es repuesto o servicio, nunca ambos]
namespace CustomCore.API.Entidades;

public class OrdenTrabajoDetalle
{
    public int IdOrdenDetalle { get; set; }
    public int OrdenTrabajoId { get; set; }
    public int? RepuestoId { get; set; }
    public int? ServicioId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal SubtotalEstimado { get; set; }

    public OrdenTrabajoEncabezado OrdenTrabajo { get; set; } = null!;
    public Repuesto? Repuesto { get; set; }
    public Servicio? Servicio { get; set; }
}
//[FIN][28/8/2026][Rodriale][Detalle de orden: una línea es repuesto o servicio, nunca ambos]