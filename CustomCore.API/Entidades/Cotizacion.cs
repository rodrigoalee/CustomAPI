//[INICIO][26/10/2026][Rodriale][Cotización de trabajo fuera del catálogo: el cliente la acepta o la rechaza desde el portal]
namespace CustomCore.API.Entidades;

public enum EstadoCotizacion
{
    Pendiente = 0,
    Aceptada = 1,
    Rechazada = 2
}

public class Cotizacion
{
    public int IdCotizacion { get; set; }
    //[INICIO][27/10/2026][Rodriale][La cotización cuelga de la orden (trabajo adicional) o de la cita (servicio fuera del catálogo, antes de la visita)]
    public int? OrdenTrabajoId { get; set; }
    public int? CitaId { get; set; }
    //[FIN][27/10/2026][Rodriale][La cotización cuelga de la orden o de la cita]
    public EstadoCotizacion Estado { get; set; } = EstadoCotizacion.Pendiente;
    public string? Nota { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaRespuesta { get; set; }
    public int UsuarioCreaId { get; set; }

    public OrdenTrabajoEncabezado? OrdenTrabajo { get; set; }
    public Cita? Cita { get; set; }
    public ICollection<CotizacionDetalle> Detalles { get; } = new List<CotizacionDetalle>();
}

public class CotizacionDetalle
{
    public int IdCotizacionDetalle { get; set; }
    public int CotizacionId { get; set; }
    public required string Descripcion { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
//[FIN][26/10/2026][Rodriale][Cotización de trabajo fuera del catálogo]
