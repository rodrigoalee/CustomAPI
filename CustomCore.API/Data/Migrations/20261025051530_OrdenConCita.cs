using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrdenConCita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CitaId",
                schema: "customcore",
                table: "OrdenesTrabajoEncabezado",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajoEncabezado_CitaId",
                schema: "customcore",
                table: "OrdenesTrabajoEncabezado",
                column: "CitaId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesTrabajoEncabezado_Citas_CitaId",
                schema: "customcore",
                table: "OrdenesTrabajoEncabezado",
                column: "CitaId",
                principalSchema: "customcore",
                principalTable: "Citas",
                principalColumn: "IdCita",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesTrabajoEncabezado_Citas_CitaId",
                schema: "customcore",
                table: "OrdenesTrabajoEncabezado");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesTrabajoEncabezado_CitaId",
                schema: "customcore",
                table: "OrdenesTrabajoEncabezado");

            migrationBuilder.DropColumn(
                name: "CitaId",
                schema: "customcore",
                table: "OrdenesTrabajoEncabezado");
        }
    }
}
