//[INICIO][28/8/2026][Rodriale][Entidad Usuario, el Correo funciona como username del login]
namespace CustomCore.API.Entidades;

public class Usuario
{
    public int IdUsuario { get; set; }
    public required string NombreCompleto { get; set; }
    public required string Correo { get; set; }
    //[INICIO][23/10/2026][Rodriale][Opcional: una cuenta de Google no tiene contraseña y nunca se le pide una]
    public string? PasswordHash { get; set; }
    //[FIN][23/10/2026][Rodriale][Opcional: una cuenta de Google no tiene contraseña]
    public bool Activo { get; set; } = true;

    //[INICIO][23/10/2026][Rodriale][Cuenta de Google: el sub es la llave, no el correo, porque el correo puede cambiar y el sub no]
    public string? GoogleId { get; set; }
    //[FIN][23/10/2026][Rodriale][Cuenta de Google]

    //[INICIO][23/10/2026][Rodriale][De aquí sale cred_ver. Antes salía del PasswordHash, que una cuenta de Google no tiene. Regenerarla mata las sesiones vivas]
    public string VersionCredenciales { get; set; } = Guid.NewGuid().ToString("N");
    //[FIN][23/10/2026][Rodriale][De aquí sale cred_ver]

    //[INICIO][12/10/2026][Rodriale][Sede del empleado; vacía solo para quien tenga sucursales.todas]
    public int? SucursalId { get; set; }
    //[FIN][12/10/2026][Rodriale][Sede del empleado]

    //[INICIO][14/10/2026][Rodriale][Si la cuenta es de un cliente, aquí está su ficha; los empleados lo dejan vacío]
    public int? ClienteId { get; set; }
    //[FIN][14/10/2026][Rodriale][Si la cuenta es de un cliente, aquí está su ficha]

    public ICollection<UsuarioRol> UsuarioRoles { get; } = new List<UsuarioRol>();
}
//[FIN][28/8/2026][Rodriale][Entidad Usuario, el Correo funciona como username del login]