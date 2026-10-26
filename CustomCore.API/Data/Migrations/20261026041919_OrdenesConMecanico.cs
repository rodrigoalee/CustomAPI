using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrdenesConMecanico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoIngreso",
                schema: "customcore",
                table: "OrdenesTrabajoEncabezado",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            //[INICIO][25/10/2026][Rodriale][Permiso de mecánico y rol Mecánico, sin duplicar si ya existen]
            migrationBuilder.Sql("""
                INSERT INTO customcore."Permisos" ("NombreCodigo", "Descripcion", "Modulo")
                SELECT 'ordenes.trabajar', 'Recibir órdenes asignadas y trabajarlas', 'Ordenes'
                WHERE NOT EXISTS (SELECT 1 FROM customcore."Permisos" x WHERE x."NombreCodigo" = 'ordenes.trabajar');

                INSERT INTO customcore."Roles" ("Nombre", "Descripcion")
                SELECT 'Mecánico', 'Trabaja las órdenes que tiene asignadas'
                WHERE NOT EXISTS (SELECT 1 FROM customcore."Roles" r WHERE r."Nombre" = 'Mecánico');

                INSERT INTO customcore."RolPermisos" ("RolId", "PermisoId")
                SELECT r."IdRol", p."IdPermiso"
                FROM customcore."Roles" r
                JOIN customcore."Permisos" p ON p."NombreCodigo" IN
                    ('ordenes.leer', 'ordenes.editar', 'ordenes.cambiar_estado', 'ordenes.trabajar', 'servicios.leer', 'inventario.leer')
                WHERE r."Nombre" = 'Mecánico'
                  AND NOT EXISTS (
                      SELECT 1 FROM customcore."RolPermisos" rp
                      WHERE rp."RolId" = r."IdRol" AND rp."PermisoId" = p."IdPermiso");
                """);
            //[FIN][25/10/2026][Rodriale][Permiso de mecánico y rol Mecánico]
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotivoIngreso",
                schema: "customcore",
                table: "OrdenesTrabajoEncabezado");
        }
    }
}
