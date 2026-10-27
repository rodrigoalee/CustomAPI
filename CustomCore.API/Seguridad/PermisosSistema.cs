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
    //[INICIO][25/10/2026][Rodriale][Marca al mecánico: puede recibir órdenes; sin ordenes.crear solo ve y toca las suyas]
    public const string OrdenesTrabajar = "ordenes.trabajar";
    //[FIN][25/10/2026][Rodriale][Marca al mecánico]
    //[INICIO][26/10/2026][Rodriale][Cotizar trabajo fuera del catálogo y enviarlo al cliente]
    public const string OrdenesCotizar = "ordenes.cotizar";
    //[FIN][26/10/2026][Rodriale][Cotizar trabajo fuera del catálogo]
    //[INICIO][27/10/2026][Rodriale][Asignar o reasignar el mecánico de una orden; separado de recepción]
    public const string OrdenesAsignar = "ordenes.asignar";
    //[FIN][27/10/2026][Rodriale][Asignar o reasignar el mecánico de una orden]
    //[INICIO][27/10/2026][Rodriale][Descargar reportes en Excel o PDF]
    public const string ReportesVer = "reportes.ver";
    //[FIN][27/10/2026][Rodriale][Descargar reportes en Excel o PDF]
    //[INICIO][26/10/2026][Rodriale][Ver el tablero de analíticas; lo usa el front, el reporte vive en Power BI]
    public const string AnaliticasVer = "analiticas.ver";
    //[FIN][26/10/2026][Rodriale][Ver el tablero de analíticas]
    //[FIN][17/9/2026][jgarciad8][Permisos de los módulos del taller]

    //[INICIO][17/9/2026][jgarciad8][Permisos de facturación y pagos]
    public const string FacturasLeer = "facturas.leer";
    public const string FacturasCrear = "facturas.crear";
    public const string FacturasRegistrarPagoManual = "facturas.registrar_pago_manual";
    public const string PagosCrearSesion = "pagos.crear_sesion";
    public const string PagosCobrar = "pagos.cobrar";
    //[FIN][17/9/2026][jgarciad8][Permisos de facturación y pagos]

    //[INICIO][12/10/2026][Rodriale][Permisos de sucursales e inventario por sede]
    public const string SucursalesLeer = "sucursales.leer";
    public const string SucursalesGestionar = "sucursales.gestionar";

    //[INICIO][12/10/2026][Rodriale][No protege endpoints: SucursalActual lo consulta para saber si quien llama ve todas las sedes]
    public const string SucursalesTodas = "sucursales.todas";
    //[FIN][12/10/2026][Rodriale][No protege endpoints]

    public const string InventarioLeer = "inventario.leer";
    public const string InventarioAjustar = "inventario.ajustar";
    //[FIN][12/10/2026][Rodriale][Permisos de sucursales e inventario por sede]



}
//[FIN][16/9/2026][jgarciad8][Códigos de permisos utilizados por el sistema]