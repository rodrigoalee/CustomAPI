//[INICIO][28/8/2026][Rodriale][Entidad Usuario, el Correo funciona como username del login]
namespace CustomCore.API.Entidades;

public class Usuario
{
    public int IdUsuario { get; set; }
    public required string NombreCompleto { get; set; }
    public required string Correo { get; set; }
    public required string PasswordHash { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<UsuarioRol> UsuarioRoles { get; } = new List<UsuarioRol>();
}
//[FIN][28/8/2026][Rodriale][Entidad Usuario, el Correo funciona como username del login]