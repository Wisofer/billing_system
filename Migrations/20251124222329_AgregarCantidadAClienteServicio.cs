using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace billing_system.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCantidadAClienteServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Cantidad",
                table: "ClienteServicios",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "ClienteServicios");
        }
    }
}
