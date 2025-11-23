using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace billing_system.Migrations
{
    /// <inheritdoc />
    public partial class AgregarServicioIdAClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServicioId",
                table: "Clientes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_ServicioId",
                table: "Clientes",
                column: "ServicioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Servicios_ServicioId",
                table: "Clientes",
                column: "ServicioId",
                principalTable: "Servicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_Servicios_ServicioId",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_ServicioId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "ServicioId",
                table: "Clientes");
        }
    }
}
