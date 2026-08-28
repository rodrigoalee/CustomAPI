//[INICIO][28/8/2026][Rodriale][Entidad Cita con duración para detectar solapamientos en agenda]
namespace CustomCore.API.Entidades;

public class Cita
{
    public int IdCita { get; set; }
    public int VehiculoId { get; set; }
    public DateTimeOffset FechaHora { get; set; }
    public int DuracionMinutos { get; set; } = 60;
    public string? Motivo { get; set; }
    public EstadoCita Estado { get; set; } = EstadoCita.Pendiente;

    public Vehiculo Vehiculo { get; set; } = null!;
}
//[FIN][28/8/2026][Rodriale][Entidad Cita con duración para detectar solapamientos en agenda]