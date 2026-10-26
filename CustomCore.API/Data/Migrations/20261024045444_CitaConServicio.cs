using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class CitaConServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServicioId",
                schema: "customcore",
                table: "Citas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Citas_ServicioId",
                schema: "customcore",
                table: "Citas",
                column: "ServicioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Citas_Servicios_ServicioId",
                schema: "customcore",
                table: "Citas",
                column: "ServicioId",
                principalSchema: "customcore",
                principalTable: "Servicios",
                principalColumn: "IdServicio",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Citas_Servicios_ServicioId",
                schema: "customcore",
                table: "Citas");

            migrationBuilder.DropIndex(
                name: "IX_Citas_ServicioId",
                schema: "customcore",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "ServicioId",
                schema: "customcore",
                table: "Citas");
        }
    }
}
