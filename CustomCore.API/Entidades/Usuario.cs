//[INICIO][28/8/2026][Rodriale][Entidad Usuario, el Correo funciona como username del login]
namespace CustomCore.API.Entidades;

public class Usuario
{
    public int IdUsuario { get; set; }
    public required string NombreCompleto { get; set; }
    public required string Correo { get; set; }
    public required string PasswordHash { get; set; }
    public bool Activo { get; set; } = true;

    //[INICIO][12/10/2026][Rodriale][Sede del empleado; vacía solo para quien tenga sucursales.todas]
    public int? SucursalId { get; set; }
    //[FIN][12/10/2026][Rodriale][Sede del empleado]

    public ICollection<UsuarioRol> UsuarioRoles { get; } = new List<UsuarioRol>();
}
//[FIN][28/8/2026][Rodriale][Entidad Usuario, el Correo funciona como username del login]