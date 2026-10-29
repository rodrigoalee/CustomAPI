using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class FotosOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FotosOrden",
                schema: "customcore",
                columns: table => new
                {
                    IdFotoOrden = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrdenTrabajoId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FechaSubida = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsuarioSubeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotosOrden", x => x.IdFotoOrden);
                    table.ForeignKey(
                        name: "FK_FotosOrden_OrdenesTrabajoEncabezado_OrdenTrabajoId",
                        column: x => x.OrdenTrabajoId,
                        principalSchema: "customcore",
                        principalTable: "OrdenesTrabajoEncabezado",
                        principalColumn: "IdOrdenEncabezado",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FotosOrden_Usuarios_UsuarioSubeId",
                        column: x => x.UsuarioSubeId,
                        principalSchema: "customcore",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FotosOrden_OrdenTrabajoId",
                schema: "customcore",
                table: "FotosOrden",
                column: "OrdenTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_FotosOrden_UsuarioSubeId",
                schema: "customcore",
                table: "FotosOrden",
                column: "UsuarioSubeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FotosOrden",
                schema: "customcore");
        }
    }
}
