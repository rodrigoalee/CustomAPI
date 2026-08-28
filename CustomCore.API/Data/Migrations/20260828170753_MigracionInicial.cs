using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomCore.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "customcore");

            migrationBuilder.CreateTable(
                name: "Clientes",
                schema: "customcore",
                columns: table => new
                {
                    IdCliente = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreCompleto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Correo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Nit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.IdCliente);
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                schema: "customcore",
                columns: table => new
                {
                    IdPermiso = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreCodigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Modulo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.IdPermiso);
                });

            migrationBuilder.CreateTable(
                name: "Repuestos",
                schema: "customcore",
                columns: table => new
                {
                    IdRepuesto = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoStock = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CantidadStock = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repuestos", x => x.IdRepuesto);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "customcore",
                columns: table => new
                {
                    IdRol = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.IdRol);
                });

            migrationBuilder.CreateTable(
                name: "Servicios",
                schema: "customcore",
                columns: table => new
                {
                    IdServicio = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TarifaManoObra = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicios", x => x.IdServicio);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                schema: "customcore",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreCompleto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Correo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                });

            migrationBuilder.CreateTable(
                name: "Vehiculos",
                schema: "customcore",
                columns: table => new
                {
                    IdVehiculo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    Placa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Marca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Modelo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Anio = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehiculos", x => x.IdVehiculo);
                    table.ForeignKey(
                        name: "FK_Vehiculos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalSchema: "customcore",
                        principalTable: "Clientes",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolPermisos",
                schema: "customcore",
                columns: table => new
                {
                    RolId = table.Column<int>(type: "integer", nullable: false),
                    PermisoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolPermisos", x => new { x.RolId, x.PermisoId });
                    table.ForeignKey(
                        name: "FK_RolPermisos_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalSchema: "customcore",
                        principalTable: "Permisos",
                        principalColumn: "IdPermiso",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolPermisos_Roles_RolId",
                        column: x => x.RolId,
                        principalSchema: "customcore",
                        principalTable: "Roles",
                        principalColumn: "IdRol",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogsAcciones",
                schema: "customcore",
                columns: table => new
                {
                    IdLog = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    TablaAfectada = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Accion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LlavePrimaria = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ValoresAnteriores = table.Column<string>(type: "jsonb", nullable: true),
                    ValoresNuevos = table.Column<string>(type: "jsonb", nullable: true),
                    FechaHora = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsAcciones", x => x.IdLog);
                    table.ForeignKey(
                        name: "FK_LogsAcciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "customcore",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioRoles",
                schema: "customcore",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    RolId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRoles", x => new { x.UsuarioId, x.RolId });
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Roles_RolId",
                        column: x => x.RolId,
                        principalSchema: "customcore",
                        principalTable: "Roles",
                        principalColumn: "IdRol",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "customcore",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Citas",
                schema: "customcore",
                columns: table => new
                {
                    IdCita = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VehiculoId = table.Column<int>(type: "integer", nullable: false),
                    FechaHora = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    Motivo = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citas", x => x.IdCita);
                    table.ForeignKey(
                        name: "FK_Citas_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalSchema: "customcore",
                        principalTable: "Vehiculos",
                        principalColumn: "IdVehiculo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesTrabajoEncabezado",
                schema: "customcore",
                columns: table => new
                {
                    IdOrdenEncabezado = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VehiculoId = table.Column<int>(type: "integer", nullable: false),
                    FechaIngreso = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FechaFinalizacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Diagnostico = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalEstimado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UsuarioRecepcionId = table.Column<int>(type: "integer", nullable: false),
                    MecanicoAsignadoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesTrabajoEncabezado", x => x.IdOrdenEncabezado);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajoEncabezado_Usuarios_MecanicoAsignadoId",
                        column: x => x.MecanicoAsignadoId,
                        principalSchema: "customcore",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajoEncabezado_Usuarios_UsuarioRecepcionId",
                        column: x => x.UsuarioRecepcionId,
                        principalSchema: "customcore",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajoEncabezado_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalSchema: "customcore",
                        principalTable: "Vehiculos",
                        principalColumn: "IdVehiculo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FacturasEncabezado",
                schema: "customcore",
                columns: table => new
                {
                    IdFacturaEncabezado = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrdenTrabajoId = table.Column<int>(type: "integer", nullable: false),
                    FechaEmision = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    NombreFacturacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NitFacturacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TasaImpuesto = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    MontoImpuesto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPagado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StripePaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EstadoPago = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UsuarioCajeroId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasEncabezado", x => x.IdFacturaEncabezado);
                    table.ForeignKey(
                        name: "FK_FacturasEncabezado_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalSchema: "customcore",
                        principalTable: "Clientes",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacturasEncabezado_OrdenesTrabajoEncabezado_OrdenTrabajoId",
                        column: x => x.OrdenTrabajoId,
                        principalSchema: "customcore",
                        principalTable: "OrdenesTrabajoEncabezado",
                        principalColumn: "IdOrdenEncabezado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacturasEncabezado_Usuarios_UsuarioCajeroId",
                        column: x => x.UsuarioCajeroId,
                        principalSchema: "customcore",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesTrabajoDetalle",
                schema: "customcore",
                columns: table => new
                {
                    IdOrdenDetalle = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrdenTrabajoId = table.Column<int>(type: "integer", nullable: false),
                    RepuestoId = table.Column<int>(type: "integer", nullable: true),
                    ServicioId = table.Column<int>(type: "integer", nullable: true),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SubtotalEstimado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesTrabajoDetalle", x => x.IdOrdenDetalle);
                    table.CheckConstraint("CK_OrdenesTrabajoDetalle_CantidadPositiva", "\"Cantidad\" > 0");
                    table.CheckConstraint("CK_OrdenesTrabajoDetalle_RepuestoOServicio", "(\"RepuestoId\" IS NOT NULL) <> (\"ServicioId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajoDetalle_OrdenesTrabajoEncabezado_OrdenTrabajo~",
                        column: x => x.OrdenTrabajoId,
                        principalSchema: "customcore",
                        principalTable: "OrdenesTrabajoEncabezado",
                        principalColumn: "IdOrdenEncabezado",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajoDetalle_Repuestos_RepuestoId",
                        column: x => x.RepuestoId,
                        principalSchema: "customcore",
                        principalTable: "Repuestos",
                        principalColumn: "IdRepuesto",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajoDetalle_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalSchema: "customcore",
                        principalTable: "Servicios",
                        principalColumn: "IdServicio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FacturasDetalle",
                schema: "customcore",
                columns: table => new
                {
                    IdFacturaDetalle = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FacturaId = table.Column<int>(type: "integer", nullable: false),
                    DescripcionItem = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasDetalle", x => x.IdFacturaDetalle);
                    table.CheckConstraint("CK_FacturasDetalle_CantidadPositiva", "\"Cantidad\" > 0");
                    table.ForeignKey(
                        name: "FK_FacturasDetalle_FacturasEncabezado_FacturaId",
                        column: x => x.FacturaId,
                        principalSchema: "customcore",
                        principalTable: "FacturasEncabezado",
                        principalColumn: "IdFacturaEncabezado",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_FechaHora",
                schema: "customcore",
                table: "Citas",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_VehiculoId_FechaHora",
                schema: "customcore",
                table: "Citas",
                columns: new[] { "VehiculoId", "FechaHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Correo",
                schema: "customcore",
                table: "Clientes",
                column: "Correo");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasDetalle_FacturaId",
                schema: "customcore",
                table: "FacturasDetalle",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasEncabezado_ClienteId",
                schema: "customcore",
                table: "FacturasEncabezado",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasEncabezado_FechaEmision",
                schema: "customcore",
                table: "FacturasEncabezado",
                column: "FechaEmision");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasEncabezado_OrdenTrabajoId",
                schema: "customcore",
                table: "FacturasEncabezado",
                column: "OrdenTrabajoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacturasEncabezado_UsuarioCajeroId",
                schema: "customcore",
                table: "FacturasEncabezado",
                column: "UsuarioCajeroId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAcciones_FechaHora",
                schema: "customcore",
                table: "LogsAcciones",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAcciones_TablaAfectada_LlavePrimaria",
                schema: "customcore",
                table: "LogsAcciones",
                columns: new[] { "TablaAfectada", "LlavePrimaria" });

            migrationBuilder.CreateIndex(
                name: "IX_LogsAcciones_UsuarioId",
                schema: "customcore",
                table: "LogsAcciones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajoDetalle_OrdenTrabajoId",
                schema: "customcore",
                table: "OrdenesTrabajoDetalle",
                column: "OrdenTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajoDetalle_RepuestoId",
                schema: "customcore",
                table: "OrdenesTrabajoDetalle",
                column: "RepuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajoDetalle_ServicioId",
                schema: "customcore",
                table: "OrdenesTrabajoDetalle",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajoEncabezado_Estado",
                schema: "customcore",
                table: "OrdenesTrabajoEncabezado",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajoEncabezado_MecanicoAsignadoId",
                schema: "customcore",
                table: "OrdenesTrabajoEncabezado",
                column: "MecanicoAsignadoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajoEncabezado_UsuarioRecepcionId",
                schema: "customcore",
                table: "OrdenesTrabajoEncabezado",
                column: "UsuarioRecepcionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajoEncabezado_VehiculoId",
                schema: "customcore",
                table: "OrdenesTrabajoEncabezado",
                column: "VehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Modulo",
                schema: "customcore",
                table: "Permisos",
                column: "Modulo");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_NombreCodigo",
                schema: "customcore",
                table: "Permisos",
                column: "NombreCodigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repuestos_CodigoStock",
                schema: "customcore",
                table: "Repuestos",
                column: "CodigoStock",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nombre",
                schema: "customcore",
                table: "Roles",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_PermisoId",
                schema: "customcore",
                table: "RolPermisos",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_RolId",
                schema: "customcore",
                table: "UsuarioRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                schema: "customcore",
                table: "Usuarios",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_ClienteId",
                schema: "customcore",
                table: "Vehiculos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Placa",
                schema: "customcore",
                table: "Vehiculos",
                column: "Placa",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Citas",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "FacturasDetalle",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "LogsAcciones",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "OrdenesTrabajoDetalle",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "RolPermisos",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "UsuarioRoles",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "FacturasEncabezado",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "Repuestos",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "Servicios",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "Permisos",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "OrdenesTrabajoEncabezado",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "Usuarios",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "Vehiculos",
                schema: "customcore");

            migrationBuilder.DropTable(
                name: "Clientes",
                schema: "customcore");
        }
    }
}
