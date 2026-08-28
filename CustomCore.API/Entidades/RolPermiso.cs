//[INICIO][28/8/2026][Rodriale][Tabla puente entre Roles y Permisos]
namespace CustomCore.API.Entidades;

public class RolPermiso
{
    public int RolId { get; set; }
    public int PermisoId { get; set; }

    public Rol Rol { get; set; } = null!;
    public Permiso Permiso { get; set; } = null!;
}
//[FIN][28/8/2026][Rodriale][Tabla puente entre Roles y Permisos]