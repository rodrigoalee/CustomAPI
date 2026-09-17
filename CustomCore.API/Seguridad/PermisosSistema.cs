//[INICIO][16/9/2026][jgarciad8][Códigos de permisos utilizados por el sistema]
namespace CustomCore.API.Seguridad;

public static class PermisosSistema
{
    public const string UsuariosLeer = "usuarios.leer";

    //[INICIO][16/9/2026][jgarciad8][Permiso para crear usuarios]
    public const string UsuariosCrear = "usuarios.crear";
    //[FIN][16/9/2026][jgarciad8][Permiso para crear usuarios]

    //[INICIO][16/9/2026][jgarciad8][Permisos para editar usuarios y administrar su estado]
    public const string UsuariosEditar = "usuarios.editar";
    public const string UsuariosCambiarEstado = "usuarios.cambiar_estado";
    //[FIN][16/9/2026][jgarciad8][Permisos para editar usuarios y administrar su estado]

    //[INICIO][16/9/2026][jgarciad8][Permisos de administración de roles]
    public const string RolesLeer = "roles.leer";
    public const string RolesCrear = "roles.crear";
    public const string RolesEditar = "roles.editar";
    public const string RolesEliminar = "roles.eliminar";
    public const string UsuariosAsignarRoles = "usuarios.asignar_roles";
    //[FIN][16/9/2026][jgarciad8][Permisos de administración de roles]


    //[INICIO][16/9/2026][jgarciad8][Permisos del catálogo y asignación a roles]
    public const string PermisosLeer = "permisos.leer";
    public const string PermisosCrear = "permisos.crear";
    public const string PermisosEditar = "permisos.editar";
    public const string PermisosEliminar = "permisos.eliminar";
    public const string RolesAsignarPermisos = "roles.asignar_permisos";
    //[FIN][16/9/2026][jgarciad8][Permisos del catálogo y asignación a roles]

}
//[FIN][16/9/2026][jgarciad8][Códigos de permisos utilizados por el sistema]