//[INICIO][29/10/2026][Rodriale][Foto del vehículo que el mecánico adjunta al diagnóstico; el archivo vive en Vercel Blob y aquí solo su URL]
namespace CustomCore.API.Entidades;

public class FotoOrden
{
    public int IdFotoOrden { get; set; }
    public int OrdenTrabajoId { get; set; }
    public required string Url { get; set; }
    public string? Descripcion { get; set; }
    public DateTimeOffset FechaSubida { get; set; }
    public int UsuarioSubeId { get; set; }

    public OrdenTrabajoEncabezado OrdenTrabajo { get; set; } = null!;
}
//[FIN][29/10/2026][Rodriale][Foto del vehículo que el mecánico adjunta al diagnóstico]
