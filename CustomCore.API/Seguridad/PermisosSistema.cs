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
}
//[FIN][16/9/2026][jgarciad8][Códigos de permisos utilizados por el sistema]