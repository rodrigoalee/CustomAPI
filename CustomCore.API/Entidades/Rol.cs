//[INICIO][28/8/2026][Rodriale][Entidad Rol del módulo de seguridad]
namespace CustomCore.API.Entidades;

public class Rol
{
    public int IdRol { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }

    public ICollection<UsuarioRol> UsuarioRoles { get; } = new List<UsuarioRol>();
    public ICollection<RolPermiso> RolPermisos { get; } = new List<RolPermiso>();
}
//[FIN][28/8/2026][Rodriale][Entidad Rol del módulo de seguridad]