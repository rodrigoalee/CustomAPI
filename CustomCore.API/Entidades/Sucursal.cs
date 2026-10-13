//[INICIO][17/9/2026][Rodriale][Entidad Sucursal cada sede física del taller]
namespace CustomCore.API.Entidades;

public class Sucursal
{
    public int IdSucursal { get; set; }
    public required string Nombre { get; set; }
    public required string Direccion { get; set; }
    public string? Telefono { get; set; }

    //[INICIO][17/9/2026][Rodriale][Baja lógica una sede cerrada sigue apareciendo en las órdenes y facturas que emitió]
    public bool Activo { get; set; } = true;
    //[FIN][17/9/2026][Rodriale][Baja lógica una sede cerrada sigue apareciendo en las órdenes y facturas que emitió]
}
//[FIN][17/9/2026][Rodriale][Entidad Sucursal cada sede física del taller]