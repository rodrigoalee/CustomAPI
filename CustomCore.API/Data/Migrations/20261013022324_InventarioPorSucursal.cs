using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InventarioPorSucursal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //[INICIO][12/10/2026][Rodriale][1. Estructura nueva; las columnas de sede nacen opcionales para poder rellenarlas]
            migrationBuilder.CreateTable(
                name: "InventariosSucursal",
                schema: "customcore",
                columns: table => new
                {
                    IdInventario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: false),
                    RepuestoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventariosSucursal", x => x.IdInventario);
                    table.CheckConstraint("CK_InventariosSucursal_CantidadNoNegativa", "\"Cantidad\" >= 0");
                    table.ForeignKey(
                        name: "FK_InventariosSucursal_Repuestos_RepuestoId",
                        column: x => x.RepuestoId,
                        principalSchema: "customcore",
                        principalTable: "Repuestos",
                        principalColumn: "IdRepuesto",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventariosSucursal_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalSchema: "customcore",
                        principalTable: "Sucursales",
                        principalColumn: "IdSucursal",
                        onDelete: ReferentialAction.Restrict);
                });

            foreach (var tabla in new[] { "Usuarios", "OrdenesTrabajoEncabezado", "Citas" })
            {
                migrationBuilder.AddColumn<int>(
                    name: "SucursalId",
                    schema: "customcore",
                    table: tabla,
                    type: "integer",
                    nullable: true);
            }
            //[FIN][12/10/2026][Rodriale][1. Estructura nueva]

            //[INICIO][12/10/2026][Rodriale][2-4. Sede por defecto y traslado: el stock actual pasa a esa sede, junto con órdenes, citas y empleados (no los Gerentes)]
            migrationBuilder.Sql("""
                INSERT INTO customcore."Sucursales" ("Nombre", "Direccion", "Activo")
                SELECT 'Central', 'Por definir', TRUE
                WHERE NOT EXISTS (SELECT 1 FROM customcore."Sucursales");

                INSERT INTO customcore."InventariosSucursal" ("SucursalId", "RepuestoId", "Cantidad")
                SELECT (SELECT MIN("IdSucursal") FROM customcore."Sucursales"), r."IdRepuesto", r."CantidadStock"
                FROM customcore."Repuestos" r;

                UPDATE customcore."OrdenesTrabajoEncabezado"
                SET "SucursalId" = (SELECT MIN("IdSucursal") FROM customcore."Sucursales");

                UPDATE customcore."Citas"
                SET "SucursalId" = (SELECT MIN("IdSucursal") FROM customcore."Sucursales");

                UPDATE customcore."Usuarios" u
                SET "SucursalId" = (SELECT MIN("IdSucursal") FROM customcore."Sucursales")
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM customcore."UsuarioRoles" ur
                    JOIN customcore."Roles" ro ON ro."IdRol" = ur."RolId"
                    WHERE ur."UsuarioId" = u."IdUsuario" AND ro."Nombre" = 'Gerente');
                """);
            //[FIN][12/10/2026][Rodriale][2-4. Sede por defecto y traslado]

            //[INICIO][12/10/2026][Rodriale][5. Con los datos ya rellenos, las restricciones y la columna vieja fuera]
            foreach (var tabla in new[] { "OrdenesTrabajoEncabezado", "Citas" })
            {
                migrationBuilder.AlterColumn<int>(
                    name: "SucursalId",
                    schema: "customcore",
                    table: tabla,
                    type: "integer",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "integer",
                    oldNullable: true);
            }

            foreach (var tabla in new[] { "Usuarios", "OrdenesTrabajoEncabezado", "Citas" })
            {
                migrationBuilder.CreateIndex(
                    name: $"IX_{tabla}_SucursalId",
                    schema: "customcore",
                    table: tabla,
                    column: "SucursalId");

                migrationBuilder.AddForeignKey(
                    name: $"FK_{tabla}_Sucursales_SucursalId",
                    schema: "customcore",
                    table: tabla,
                    column: "SucursalId",
                    principalSchema: "customcore",
                    principalTable: "Sucursales",
                    principalColumn: "IdSucursal",
                    onDelete: ReferentialAction.Restrict);
            }

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSucursal_RepuestoId",
                schema: "customcore",
                table: "InventariosSucursal",
                column: "RepuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSucursal_SucursalId_RepuestoId",
                schema: "customcore",
                table: "InventariosSucursal",
                columns: new[] { "SucursalId", "RepuestoId" },
                unique: true);

            migrationBuilder.DropColumn(
                name: "CantidadStock",
                schema: "customcore",
                table: "Repuestos");
            //[FIN][12/10/2026][Rodriale][5. Restricciones y columna vieja fuera]

            //[INICIO][12/10/2026][Rodriale][6. Permisos nuevos, sin duplicar, y asignados al Gerente para que nadie pierda acceso al migrar]
            migrationBuilder.Sql("""
                INSERT INTO customcore."Permisos" ("NombreCodigo", "Descripcion", "Modulo")
                SELECT p.codigo, p.descripcion, p.modulo
                FROM (VALUES
                    ('sucursales.leer', 'Ver el catálogo de sucursales', 'Sucursales'),
                    ('sucursales.gestionar', 'Crear, editar y cerrar sucursales', 'Sucursales'),
                    ('sucursales.todas', 'Operar y ver datos de todas las sucursales', 'Sucursales'),
                    ('inventario.leer', 'Ver existencias por sucursal', 'Inventario'),
                    ('inventario.ajustar', 'Fijar existencias de un repuesto en una sucursal', 'Inventario')
                ) AS p(codigo, descripcion, modulo)
                WHERE NOT EXISTS (SELECT 1 FROM customcore."Permisos" x WHERE x."NombreCodigo" = p.codigo);

                INSERT INTO customcore."RolPermisos" ("RolId", "PermisoId")
                SELECT r."IdRol", p."IdPermiso"
                FROM customcore."Roles" r
                JOIN customcore."Permisos" p ON p."NombreCodigo" IN
                    ('sucursales.leer', 'sucursales.gestionar', 'sucursales.todas', 'inventario.leer', 'inventario.ajustar')
                WHERE r."Nombre" = 'Gerente'
                  AND NOT EXISTS (
                      SELECT 1 FROM customcore."RolPermisos" rp
                      WHERE rp."RolId" = r."IdRol" AND rp."PermisoId" = p."IdPermiso");
                """);
            //[FIN][12/10/2026][Rodriale][6. Permisos nuevos]
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //[INICIO][12/10/2026][Rodriale][Vuelve al stock único sumando todas las sedes, para revertir sin perder existencias. Los permisos se quedan: pudieron asignarse a mano]
            migrationBuilder.AddColumn<int>(
                name: "CantidadStock",
                schema: "customcore",
                table: "Repuestos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE customcore."Repuestos" r
                SET "CantidadStock" = COALESCE(
                    (SELECT SUM(i."Cantidad") FROM customcore."InventariosSucursal" i WHERE i."RepuestoId" = r."IdRepuesto"), 0);
                """);

            foreach (var tabla in new[] { "Usuarios", "OrdenesTrabajoEncabezado", "Citas" })
            {
                migrationBuilder.DropForeignKey(
                    name: $"FK_{tabla}_Sucursales_SucursalId",
                    schema: "customcore",
                    table: tabla);

                migrationBuilder.DropIndex(
                    name: $"IX_{tabla}_SucursalId",
                    schema: "customcore",
                    table: tabla);

                migrationBuilder.DropColumn(
                    name: "SucursalId",
                    schema: "customcore",
                    table: tabla);
            }

            migrationBuilder.DropTable(
                name: "InventariosSucursal",
                schema: "customcore");
            //[FIN][12/10/2026][Rodriale][Vuelve al stock único sumando todas las sedes]
        }
    }
}
