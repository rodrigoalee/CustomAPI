//[INICIO][28/8/2026][Rodriale][Entidad Permiso, agrupada por módulo para la UI]
namespace CustomCore.API.Entidades;

public class Permiso
{
    public int IdPermiso { get; set; }
    public required string NombreCodigo { get; set; }
    public string? Descripcion { get; set; }
    public required string Modulo { get; set; }

    public ICollection<RolPermiso> RolPermisos { get; } = new List<RolPermiso>();
}
//[FIN][28/8/2026][Rodriale][Entidad Permiso, agrupada por módulo para la UI]