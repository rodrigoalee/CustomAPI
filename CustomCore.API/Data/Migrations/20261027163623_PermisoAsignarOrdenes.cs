using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class PermisoAsignarOrdenes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //[INICIO][27/10/2026][Rodriale][Permiso de asignar mecánico para quien ve todas las sedes, sin duplicar; el resto se otorga desde roles]
            migrationBuilder.Sql("""
                INSERT INTO customcore."Permisos" ("NombreCodigo", "Descripcion", "Modulo")
                SELECT 'ordenes.asignar', 'Asignar o reasignar el mecánico de una orden', 'Ordenes'
                WHERE NOT EXISTS (SELECT 1 FROM customcore."Permisos" x WHERE x."NombreCodigo" = 'ordenes.asignar');

                INSERT INTO customcore."RolPermisos" ("RolId", "PermisoId")
                SELECT rp."RolId", p."IdPermiso"
                FROM customcore."RolPermisos" rp
                JOIN customcore."Permisos" todas ON todas."IdPermiso" = rp."PermisoId" AND todas."NombreCodigo" = 'sucursales.todas'
                JOIN customcore."Permisos" p ON p."NombreCodigo" = 'ordenes.asignar'
                WHERE NOT EXISTS (
                    SELECT 1 FROM customcore."RolPermisos" x
                    WHERE x."RolId" = rp."RolId" AND x."PermisoId" = p."IdPermiso");
                """);
            //[FIN][27/10/2026][Rodriale][Permiso de asignar mecánico]
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DELETE FROM customcore."Permisos" WHERE "NombreCodigo" = 'ordenes.asignar';""");
        }
    }
}
