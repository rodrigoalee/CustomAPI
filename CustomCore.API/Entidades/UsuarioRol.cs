//[INICIO][28/8/2026][Rodriale][Tabla puente entre Usuarios y Roles]
namespace CustomCore.API.Entidades;

public class UsuarioRol
{
    public int UsuarioId { get; set; }
    public int RolId { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Rol Rol { get; set; } = null!;
}
//[FIN][28/8/2026][Rodriale][Tabla puente entre Usuarios y Roles]