using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DNC.InternshipSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGradingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CompanyScore",
                table: "Registrations",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FinalScore",
                table: "Registrations",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ReportScore",
                table: "Registrations",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VivaScore",
                table: "Registrations",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyScore",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "FinalScore",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "ReportScore",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "VivaScore",
                table: "Registrations");
        }
    }
}
