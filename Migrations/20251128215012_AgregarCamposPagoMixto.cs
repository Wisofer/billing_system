using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace billing_system.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposPagoMixto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoCordobasElectronico",
                table: "Pagos",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoCordobasFisico",
                table: "Pagos",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDolaresElectronico",
                table: "Pagos",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDolaresFisico",
                table: "Pagos",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoRecibidoFisico",
                table: "Pagos",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VueltoFisico",
                table: "Pagos",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontoCordobasElectronico",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "MontoCordobasFisico",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "MontoDolaresElectronico",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "MontoDolaresFisico",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "MontoRecibidoFisico",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "VueltoFisico",
                table: "Pagos");
        }
    }
}
