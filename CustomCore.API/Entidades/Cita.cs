//[INICIO][28/8/2026][Rodriale][Entidad Cita con duración para detectar solapamientos en agenda]
namespace CustomCore.API.Entidades;

public class Cita
{
    public int IdCita { get; set; }
    public int VehiculoId { get; set; }

    //[INICIO][12/10/2026][Rodriale][Sede donde se agenda la cita]
    public int SucursalId { get; set; }
    //[FIN][12/10/2026][Rodriale][Sede donde se agenda la cita]

    public DateTimeOffset FechaHora { get; set; }
    public int DuracionMinutos { get; set; } = 60;
    public string? Motivo { get; set; }
    public EstadoCita Estado { get; set; } = EstadoCita.Pendiente;

    //[INICIO][23/10/2026][Rodriale][Servicio pedido desde el portal; opcional porque las citas viejas y las del taller no lo traen]
    public int? ServicioId { get; set; }
    public Servicio? Servicio { get; set; }
    //[FIN][23/10/2026][Rodriale][Servicio pedido desde el portal]

    public Vehiculo Vehiculo { get; set; } = null!;
}
//[FIN][28/8/2026][Rodriale][Entidad Cita con duración para detectar solapamientos en agenda]