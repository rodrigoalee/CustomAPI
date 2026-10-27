using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class PermisoReportes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //[INICIO][27/10/2026][Rodriale][Permiso de reportería para quien ve todas las sedes, sin duplicar; el resto se otorga desde roles]
            migrationBuilder.Sql("""
                INSERT INTO customcore."Permisos" ("NombreCodigo", "Descripcion", "Modulo")
                SELECT 'reportes.ver', 'Descargar reportes en Excel o PDF', 'Reportes'
                WHERE NOT EXISTS (SELECT 1 FROM customcore."Permisos" x WHERE x."NombreCodigo" = 'reportes.ver');

                INSERT INTO customcore."RolPermisos" ("RolId", "PermisoId")
                SELECT rp."RolId", p."IdPermiso"
                FROM customcore."RolPermisos" rp
                JOIN customcore."Permisos" todas ON todas."IdPermiso" = rp."PermisoId" AND todas."NombreCodigo" = 'sucursales.todas'
                JOIN customcore."Permisos" p ON p."NombreCodigo" = 'reportes.ver'
                WHERE NOT EXISTS (
                    SELECT 1 FROM customcore."RolPermisos" x
                    WHERE x."RolId" = rp."RolId" AND x."PermisoId" = p."IdPermiso");
                """);
            //[FIN][27/10/2026][Rodriale][Permiso de reportería]
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DELETE FROM customcore."Permisos" WHERE "NombreCodigo" = 'reportes.ver';""");
        }
    }
}
