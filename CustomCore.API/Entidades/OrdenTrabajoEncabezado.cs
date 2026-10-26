//[INICIO][28/8/2026][Rodriale][Encabezado de la orden de trabajo con cierre y responsables]
namespace CustomCore.API.Entidades;

public class OrdenTrabajoEncabezado
{
    public int IdOrdenEncabezado { get; set; }
    public int VehiculoId { get; set; }

    //[INICIO][12/10/2026][Rodriale][Sede donde se atiende la orden; de aquí sale el stock que consume]
    public int SucursalId { get; set; }
    //[FIN][12/10/2026][Rodriale][Sede donde se atiende la orden]

    public DateTimeOffset FechaIngreso { get; set; }
    public DateTimeOffset? FechaFinalizacion { get; set; }
    public string? Diagnostico { get; set; }

    //[INICIO][25/10/2026][Rodriale][Lo que reporta el cliente al dejar el carro; lo escribe recepción. El diagnóstico queda para el mecánico]
    public string? MotivoIngreso { get; set; }
    //[FIN][25/10/2026][Rodriale][Lo que reporta el cliente al dejar el carro]

    public EstadoOrdenTrabajo Estado { get; set; } = EstadoOrdenTrabajo.EnRecepcion;
    public decimal TotalEstimado { get; set; }

    //[INICIO][28/8/2026][Rodriale][Responsables: quien recibió el vehículo y el mecánico asignado]
    public int UsuarioRecepcionId { get; set; }
    public int? MecanicoAsignadoId { get; set; }
    //[FIN][28/8/2026][Rodriale][Responsables: quien recibió el vehículo y el mecánico asignado]

    //[INICIO][24/10/2026][Rodriale][Cita que originó la orden; opcional porque hay carros que llegan sin cita y órdenes viejas]
    public int? CitaId { get; set; }
    public Cita? Cita { get; set; }
    //[FIN][24/10/2026][Rodriale][Cita que originó la orden]

    public Vehiculo Vehiculo { get; set; } = null!;
    public Usuario UsuarioRecepcion { get; set; } = null!;
    public Usuario? MecanicoAsignado { get; set; }
    public ICollection<OrdenTrabajoDetalle> Detalles { get; } = new List<OrdenTrabajoDetalle>();
}
//[FIN][28/8/2026][Rodriale][Encabezado de la orden de trabajo con cierre y responsables]