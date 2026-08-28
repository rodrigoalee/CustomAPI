//[INICIO][28/8/2026][Rodriale][Encabezado de la orden de trabajo con cierre y responsables]
namespace CustomCore.API.Entidades;

public class OrdenTrabajoEncabezado
{
    public int IdOrdenEncabezado { get; set; }
    public int VehiculoId { get; set; }
    public DateTimeOffset FechaIngreso { get; set; }
    public DateTimeOffset? FechaFinalizacion { get; set; }
    public string? Diagnostico { get; set; }
    public EstadoOrdenTrabajo Estado { get; set; } = EstadoOrdenTrabajo.EnRecepcion;
    public decimal TotalEstimado { get; set; }

    //[INICIO][28/8/2026][Rodriale][Responsables: quien recibió el vehículo y el mecánico asignado]
    public int UsuarioRecepcionId { get; set; }
    public int? MecanicoAsignadoId { get; set; }
    //[FIN][28/8/2026][Rodriale][Responsables: quien recibió el vehículo y el mecánico asignado]

    public Vehiculo Vehiculo { get; set; } = null!;
    public ICollection<OrdenTrabajoDetalle> Detalles { get; } = new List<OrdenTrabajoDetalle>();
}
//[FIN][28/8/2026][Rodriale][Encabezado de la orden de trabajo con cierre y responsables]