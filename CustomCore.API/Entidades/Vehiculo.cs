//[INICIO][28/8/2026][Rodriale][Entidad Vehiculo asociada a un Cliente]
namespace CustomCore.API.Entidades;

public class Vehiculo
{
    public int IdVehiculo { get; set; }
    public int ClienteId { get; set; }
    public required string Placa { get; set; }
    public required string Marca { get; set; }
    public required string Modelo { get; set; }
    public int Anio { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public ICollection<Cita> Citas { get; } = new List<Cita>();
}
//[FIN][28/8/2026][Rodriale][Entidad Vehiculo asociada a un Cliente]