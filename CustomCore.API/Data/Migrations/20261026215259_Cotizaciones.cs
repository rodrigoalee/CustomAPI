using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Cotizaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrdenesTrabajoDetalle_RepuestoOServicio",
                schema: "customcore",
                table: "OrdenesTrabajoDetalle");

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                schema: "customcore",
                table: "OrdenesTrabajoDetalle",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cotizaciones",
                schema: "customcore",
                columns: table => new
                {
                    IdCotizacion = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrdenTrabajoId = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nota = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FechaRespuesta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UsuarioCreaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cotizaciones", x => x.IdCotizacion);
                    table.ForeignKey(
                        name: "FK_Cotizaciones_OrdenesTrabajoEncabezado_OrdenTrabajoId",
                        column: x => x.OrdenTrabajoId,
                        principalSchema: "customcore",
                        principalTable: "OrdenesTrabajoEncabezado",
                        principalColumn: "IdOrdenEncabezado",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cotizaciones_Usuarios_UsuarioCreaId",
                        column: x => x.UsuarioCreaId,
                        principalSchema: "customcore",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CotizacionesDetalle",
                schema: "customcore",
                columns: table => new
                {
                    IdCotizacionDetalle = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CotizacionId = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CotizacionesDetalle", x => x.IdCotizacionDetalle);
                    table.CheckConstraint("CK_CotizacionesDetalle_CantidadPositiva", "\"Cantidad\" > 0");
                    table.CheckConstraint("CK_CotizacionesDetalle_PrecioNoNegativo", "\"PrecioUnitario\" >= 0");
                    table.ForeignKey(
                        name: "FK_CotizacionesDetalle_Cotizaciones_CotizacionId",
                        column: x => x.CotizacionId,
                        principalSchema: "customcore",
                        principalTable: "Cotizaciones",
                        principalColumn: "IdCotizacion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrdenesTrabajoDetalle_RepuestoServicioOAdicional",
                schema: "customcore",
                table: "OrdenesTrabajoDetalle",
                sql: "num_nonnulls(\"RepuestoId\", \"ServicioId\", \"Descripcion\") = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Cotizaciones_OrdenTrabajoId",
                schema: "customcore",
                table: "Cotizaciones",
                column: "OrdenTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotizaciones_UsuarioCreaId",
                schema: "customcore",
                table: "Cotizaciones",
                column: "UsuarioCreaId");

            migrationBuilder.CreateIndex(
                name: "IX_CotizacionesDetalle_CotizacionId",
                schema: "customcore",
                table: "CotizacionesDetalle",
                column: "CotizacionId");

            //[INICIO][26/10/2026][Rodriale][Permiso de cotizar para quien ya abre órdenes (recepción y gerencia), sin duplicar]
            migrationBuilder.Sql("""
                INSERT INTO customcore."Permisos" ("NombreCodigo", "Descripcion", "Modulo")
                SELECT 'ordenes.cotizar', 'Cotizar trabajo fuera del catálogo', 'Ordenes'
                WHERE NOT EXISTS (SELECT 1 FROM customcore."Permisos" x WHERE x."NombreCodigo" = 'ordenes.cotizar');

                INSERT INTO customcore."RolPermisos" ("RolId", "PermisoId")
                SELECT rp."RolId", p."IdPermiso"
                FROM customcore."RolPermisos" rp
                JOIN customcore."Permisos" crear ON crear."IdPermiso" = rp."PermisoId" AND crear."NombreCodigo" = 'ordenes.crear'
                JOIN customcore."Permisos" p ON p."NombreCodigo" = 'ordenes.cotizar'
                WHERE NOT EXISTS (
                    SELECT 1 FROM customcore."RolPermisos" x
                    WHERE x."RolId" = rp."RolId" AND x."PermisoId" = p."IdPermiso");
                """);
            //[FIN][26/10/2026][Rodriale][Permiso de cotizar]
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //[INICIO][26/10/2026][Rodriale][Al revertir, las líneas adicionales no caben en la regla anterior y se quitan]
            migrationBuilder.Sql("""
                DELETE FROM customcore."Permisos" WHERE "NombreCodigo" = 'ordenes.cotizar';
                DELETE FROM customcore."OrdenesTrabajoDetalle" WHERE "Descripcion" IS NOT NULL;
                """);
            //[FIN][26/10/2026][Rodriale][Al revertir, las líneas adicionales se quitan]

            migrationBuilder.DropTable(
                name: "CotizacionesDetalle",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "Cotizaciones",
                schema: "customcore");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrdenesTrabajoDetalle_RepuestoServicioOAdicional",
                schema: "customcore",
                table: "OrdenesTrabajoDetalle");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                schema: "customcore",
                table: "OrdenesTrabajoDetalle");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrdenesTrabajoDetalle_RepuestoOServicio",
                schema: "customcore",
                table: "OrdenesTrabajoDetalle",
                sql: "(\"RepuestoId\" IS NOT NULL) <> (\"ServicioId\" IS NOT NULL)");
        }
    }
}
