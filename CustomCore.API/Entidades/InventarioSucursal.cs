//[INICIO][12/10/2026][Rodriale][Existencias de un repuesto en una sede; el precio sigue siendo único en Repuestos]
namespace CustomCore.API.Entidades;

public class InventarioSucursal
{
    //[INICIO][12/10/2026][Rodriale][Llave simple: la auditoría bloquea y registra por una sola llave int]
    public int IdInventario { get; set; }
    //[FIN][12/10/2026][Rodriale][Llave simple]
    public int SucursalId { get; set; }
    public int RepuestoId { get; set; }
    public int Cantidad { get; set; }
}
//[FIN][12/10/2026][Rodriale][Existencias de un repuesto en una sede]
