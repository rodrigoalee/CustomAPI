using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class CuentasDeCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clientes_Correo",
                schema: "customcore",
                table: "Clientes");

            migrationBuilder.AddColumn<int>(
                name: "ClienteId",
                schema: "customcore",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ClienteId",
                schema: "customcore",
                table: "Usuarios",
                column: "ClienteId",
                unique: true,
                filter: "\"ClienteId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Correo",
                schema: "customcore",
                table: "Clientes",
                column: "Correo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Clientes_ClienteId",
                schema: "customcore",
                table: "Usuarios",
                column: "ClienteId",
                principalSchema: "customcore",
                principalTable: "Clientes",
                principalColumn: "IdCliente",
                onDelete: ReferentialAction.Restrict);

            //[INICIO][14/10/2026][Rodriale][El rol viaja dentro de la migración: sin él el registro del portal no arranca, y así ninguna base se queda sin él. Nace sin permisos a propósito]
            migrationBuilder.Sql("""
                INSERT INTO customcore."Roles" ("Nombre", "Descripcion")
                SELECT 'Cliente', 'Dueño de vehículo con acceso al portal del cliente'
                 WHERE NOT EXISTS (
                       SELECT 1 FROM customcore."Roles" WHERE "Nombre" = 'Cliente');
                """);
            //[FIN][14/10/2026][Rodriale][El rol viaja dentro de la migración]
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //[INICIO][14/10/2026][Rodriale][Solo se borra el rol si nadie lo tiene asignado: revertir no debe dejar cuentas de cliente sin rol]
            migrationBuilder.Sql("""
                DELETE FROM customcore."Roles" r
                 WHERE r."Nombre" = 'Cliente'
                   AND NOT EXISTS (
                       SELECT 1 FROM customcore."UsuarioRoles" ur
                        WHERE ur."RolId" = r."IdRol");
                """);
            //[FIN][14/10/2026][Rodriale][Solo se borra el rol si nadie lo tiene asignado]

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Clientes_ClienteId",
                schema: "customcore",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ClienteId",
                schema: "customcore",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Correo",
                schema: "customcore",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                schema: "customcore",
                table: "Usuarios");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Correo",
                schema: "customcore",
                table: "Clientes",
                column: "Correo");
        }
    }
}
