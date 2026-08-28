//[INICIO][27/8/2026][Rodriale][Creación de la sesión con la base de datos]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Data
{
    public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        //[INICIO][27/8/2026][Rodriale][definimos las entidades para que sean públicas para poder hacer consultas]
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
        public DbSet<Repuesto> Repuestos => Set<Repuesto>();
        public DbSet<Servicio> Servicios => Set<Servicio>();
        public DbSet<Cita> Citas => Set<Cita>();
        public DbSet<OrdenTrabajoEncabezado> OrdenesTrabajoEncabezado => Set<OrdenTrabajoEncabezado>();
        public DbSet<OrdenTrabajoDetalle> OrdenesTrabajoDetalle => Set<OrdenTrabajoDetalle>();
        public DbSet<FacturaEncabezado> FacturasEncabezado => Set<FacturaEncabezado>();
        public DbSet<FacturaDetalle> FacturasDetalle => Set<FacturaDetalle>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Rol> Roles => Set<Rol>();
        public DbSet<Permiso> Permisos => Set<Permiso>();
        public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
        public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
        public DbSet<LogAccion> LogsAcciones => Set<LogAccion>();
        //[FIN][27/8/2026][Rodriale][definimos las entidades para que sean públicas para poder hacer consultas]

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {


            base.OnModelCreating(modelBuilder);
            //[INICIO][28/8/2026][Rodriale][Esquema propio para que Supabase no exponga estas tablas por su API pública]
            modelBuilder.HasDefaultSchema("customcore");
            //[FIN][28/8/2026][Rodriale][Esquema propio para que Supabase no exponga estas tablas por su API pública]
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
//[FIN][27/8/2026][Rodriale][Creación de la sesión con la base de datos]