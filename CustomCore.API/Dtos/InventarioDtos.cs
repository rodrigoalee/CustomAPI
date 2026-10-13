//[INICIO][12/10/2026][Rodriale][DTOs del inventario por sucursal]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos
{
    //[INICIO][12/10/2026][Rodriale][Un repuesto del catálogo con sus existencias en la sede consultada; 0 si la sede nunca lo ha tenido]
    public record InventarioDto(
        int RepuestoId,
        string CodigoStock,
        string Nombre,
        decimal PrecioUnitario,
        int Cantidad);
    //[FIN][12/10/2026][Rodriale][Un repuesto del catálogo con sus existencias en la sede consultada]

    //[INICIO][12/10/2026][Rodriale][Valor absoluto: lo que se contó en bodega, no una suma o resta]
    public record FijarInventarioRequest(
        [Range(0, int.MaxValue)] int Cantidad);
    //[FIN][12/10/2026][Rodriale][Valor absoluto]
}
//[FIN][12/10/2026][Rodriale][DTOs del inventario por sucursal]
