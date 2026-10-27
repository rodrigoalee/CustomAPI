using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class CotizacionDeCita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OrdenTrabajoId",
                schema: "customcore",
                table: "Cotizaciones",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "CitaId",
                schema: "customcore",
                table: "Cotizaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ServicioPersonalizado",
                schema: "customcore",
                table: "Citas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Cotizaciones_CitaId",
                schema: "customcore",
                table: "Cotizaciones",
                column: "CitaId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cotizaciones_UnSoloDueno",
                schema: "customcore",
                table: "Cotizaciones",
                sql: "(\"OrdenTrabajoId\" IS NULL) <> (\"CitaId\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Cotizaciones_Citas_CitaId",
                schema: "customcore",
                table: "Cotizaciones",
                column: "CitaId",
                principalSchema: "customcore",
                principalTable: "Citas",
                principalColumn: "IdCita",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cotizaciones_Citas_CitaId",
                schema: "customcore",
                table: "Cotizaciones");

            migrationBuilder.DropIndex(
                name: "IX_Cotizaciones_CitaId",
                schema: "customcore",
                table: "Cotizaciones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Cotizaciones_UnSoloDueno",
                schema: "customcore",
                table: "Cotizaciones");

            migrationBuilder.DropColumn(
                name: "CitaId",
                schema: "customcore",
                table: "Cotizaciones");

            migrationBuilder.DropColumn(
                name: "ServicioPersonalizado",
                schema: "customcore",
                table: "Citas");

            migrationBuilder.AlterColumn<int>(
                name: "OrdenTrabajoId",
                schema: "customcore",
                table: "Cotizaciones",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
