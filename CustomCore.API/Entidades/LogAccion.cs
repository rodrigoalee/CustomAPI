//[INICIO][28/8/2026][Rodriale][Bitácora de cambios; UsuarioId nulo para acciones del sistema]
namespace CustomCore.API.Entidades;

public class LogAccion
{
    public int IdLog { get; set; }
    public int? UsuarioId { get; set; }
    public required string TablaAfectada { get; set; }
    public AccionLog Accion { get; set; }
    public required string LlavePrimaria { get; set; }
    public string? ValoresAnteriores { get; set; }
    public string? ValoresNuevos { get; set; }
    public DateTimeOffset FechaHora { get; set; }

    public Usuario? Usuario { get; set; }
}
//[FIN][28/8/2026][Rodriale][Bitácora de cambios; UsuarioId nulo para acciones del sistema]