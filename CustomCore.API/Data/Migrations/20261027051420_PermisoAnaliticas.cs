using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class PermisoAnaliticas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //[INICIO][26/10/2026][Rodriale][Permiso del módulo de analíticas (Power BI) para quien ve todas las sedes, sin duplicar]
            migrationBuilder.Sql("""
                INSERT INTO customcore."Permisos" ("NombreCodigo", "Descripcion", "Modulo")
                SELECT 'analiticas.ver', 'Ver el tablero de analíticas', 'Analiticas'
                WHERE NOT EXISTS (SELECT 1 FROM customcore."Permisos" x WHERE x."NombreCodigo" = 'analiticas.ver');

                INSERT INTO customcore."RolPermisos" ("RolId", "PermisoId")
                SELECT rp."RolId", p."IdPermiso"
                FROM customcore."RolPermisos" rp
                JOIN customcore."Permisos" todas ON todas."IdPermiso" = rp."PermisoId" AND todas."NombreCodigo" = 'sucursales.todas'
                JOIN customcore."Permisos" p ON p."NombreCodigo" = 'analiticas.ver'
                WHERE NOT EXISTS (
                    SELECT 1 FROM customcore."RolPermisos" x
                    WHERE x."RolId" = rp."RolId" AND x."PermisoId" = p."IdPermiso");
                """);
            //[FIN][26/10/2026][Rodriale][Permiso del módulo de analíticas]
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DELETE FROM customcore."Permisos" WHERE "NombreCodigo" = 'analiticas.ver';""");
        }
    }
}
