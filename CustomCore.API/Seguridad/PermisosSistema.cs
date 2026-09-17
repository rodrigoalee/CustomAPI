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



    //[INICIO][17/9/2026][jgarciad8][Permisos de los módulos del taller]
    public const string ClientesLeer = "clientes.leer";
    public const string ClientesCrear = "clientes.crear";
    public const string ClientesEditar = "clientes.editar";

    public const string VehiculosLeer = "vehiculos.leer";
    public const string VehiculosCrear = "vehiculos.crear";
    public const string VehiculosEditar = "vehiculos.editar";
    public const string VehiculosEliminar = "vehiculos.eliminar";

    public const string RepuestosLeer = "repuestos.leer";
    public const string RepuestosCrear = "repuestos.crear";
    public const string RepuestosEditar = "repuestos.editar";
    public const string RepuestosBaja = "repuestos.baja";

    public const string ServiciosLeer = "servicios.leer";
    public const string ServiciosCrear = "servicios.crear";
    public const string ServiciosEditar = "servicios.editar";
    public const string ServiciosBaja = "servicios.baja";

    public const string CitasLeer = "citas.leer";
    public const string CitasCrear = "citas.crear";
    public const string CitasEditar = "citas.editar";
    public const string CitasCambiarEstado = "citas.cambiar_estado";

    public const string OrdenesLeer = "ordenes.leer";
    public const string OrdenesCrear = "ordenes.crear";
    public const string OrdenesEditar = "ordenes.editar";
    public const string OrdenesCambiarEstado = "ordenes.cambiar_estado";
    //[FIN][17/9/2026][jgarciad8][Permisos de los módulos del taller]

    //[INICIO][17/9/2026][jgarciad8][Permisos de facturación y pagos]
    public const string FacturasLeer = "facturas.leer";
    public const string FacturasCrear = "facturas.crear";
    public const string FacturasRegistrarPagoManual = "facturas.registrar_pago_manual";
    public const string PagosCrearSesion = "pagos.crear_sesion";
    public const string PagosCobrar = "pagos.cobrar";
    //[FIN][17/9/2026][jgarciad8][Permisos de facturación y pagos]



}
//[FIN][16/9/2026][jgarciad8][Códigos de permisos utilizados por el sistema]