using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace billing_system.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMaterialesInstalacionYTiposMedida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "StockMinimo",
                table: "Equipos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stock",
                table: "Equipos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TipoMedida",
                table: "Equipos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unidad");

            migrationBuilder.AddColumn<string>(
                name: "UnidadMedida",
                table: "Equipos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "unidades");

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "Clientes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Contactos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Correo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Mensaje = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Nuevo"),
                    FechaLeido = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FechaRespondido = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ubicacion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Latitud = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    Longitud = table.Column<decimal>(type: "numeric(11,8)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contactos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialesInstalacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    EquipoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FechaInstalacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialesInstalacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialesInstalacion_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialesInstalacion_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contactos_Estado",
                table: "Contactos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Contactos_FechaEnvio",
                table: "Contactos",
                column: "FechaEnvio");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesInstalacion_ClienteId",
                table: "MaterialesInstalacion",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesInstalacion_EquipoId",
                table: "MaterialesInstalacion",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesInstalacion_FechaInstalacion",
                table: "MaterialesInstalacion",
                column: "FechaInstalacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contactos");

            migrationBuilder.DropTable(
                name: "MaterialesInstalacion");

            migrationBuilder.DropColumn(
                name: "TipoMedida",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "UnidadMedida",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "Clientes");

            migrationBuilder.AlterColumn<int>(
                name: "StockMinimo",
                table: "Equipos",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "Stock",
                table: "Equipos",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldDefaultValue: 0m);
        }
    }
}
