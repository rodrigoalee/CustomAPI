using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class CuentasConGoogle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                schema: "customcore",
                table: "Usuarios",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                schema: "customcore",
                table: "Usuarios",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionCredenciales",
                schema: "customcore",
                table: "Usuarios",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            //[INICIO][23/10/2026][Rodriale][Un valor distinto por cuenta; si todas compartieran el mismo, cred_ver dejaría de distinguir nada]
            migrationBuilder.Sql("""
                UPDATE customcore."Usuarios"
                SET "VersionCredenciales" = REPLACE(gen_random_uuid()::text, '-', '');
                """);
            //[FIN][23/10/2026][Rodriale][Un valor distinto por cuenta]

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_GoogleId",
                schema: "customcore",
                table: "Usuarios",
                column: "GoogleId",
                unique: true,
                filter: "\"GoogleId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Usuarios_AlMenosUnaCredencial",
                schema: "customcore",
                table: "Usuarios",
                sql: "\"PasswordHash\" IS NOT NULL OR \"GoogleId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_GoogleId",
                schema: "customcore",
                table: "Usuarios");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Usuarios_AlMenosUnaCredencial",
                schema: "customcore",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                schema: "customcore",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "VersionCredenciales",
                schema: "customcore",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                schema: "customcore",
                table: "Usuarios",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
