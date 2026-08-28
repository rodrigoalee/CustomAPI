//[INICIO][28/8/2026][Rodriale][Entidad Cliente del módulo de clientes y vehículos]

namespace CustomCore.API.Entidades;

public class Cliente
{
    public int IdCliente { get; set; }
    public required string NombreCompleto { get; set; }
    public required string Telefono { get; set; }
    public required string Correo { get; set; }
    public string? Nit { get; set; }

    public ICollection<Vehiculo> Vehiculos { get; } = new List<Vehiculo>();
}
//[FIN][28/8/2026][Rodriale][Entidad Cliente del módulo de clientes y vehículos]
