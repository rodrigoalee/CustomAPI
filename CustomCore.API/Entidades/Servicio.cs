//[INICIO][28/8/2026][Rodriale][Entidad Servicio (mano de obra) con borrado lógico]
namespace CustomCore.API.Entidades;

public class Servicio
{
    public int IdServicio { get; set; }
    public required string Nombre { get; set; }
    public decimal TarifaManoObra { get; set; }
    public bool Activo { get; set; } = true;
}
//[FIN][28/8/2026][Rodriale][Entidad Servicio (mano de obra) con borrado lógico]