//[INICIO][28/8/2026][Rodriale][Entidad Repuesto con borrado lógico para no romper el historial]
namespace CustomCore.API.Entidades;

public class Repuesto
{
    public int IdRepuesto { get; set; }
    public required string CodigoStock { get; set; }
    public required string Nombre { get; set; }
    public decimal PrecioUnitario { get; set; }
    public int CantidadStock { get; set; }
    public bool Activo { get; set; } = true;
}
//[FIN][28/8/2026][Rodriale][Entidad Repuesto con borrado lógico para no romper el historial]