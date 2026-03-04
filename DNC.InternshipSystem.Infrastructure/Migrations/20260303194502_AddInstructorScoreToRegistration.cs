using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DNC.InternshipSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorScoreToRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportScore",
                table: "Registrations");

            migrationBuilder.RenameColumn(
                name: "VivaScore",
                table: "Registrations",
                newName: "InstructorScore");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InstructorScore",
                table: "Registrations",
                newName: "VivaScore");

            migrationBuilder.AddColumn<double>(
                name: "ReportScore",
                table: "Registrations",
                type: "float",
                nullable: true);
        }
    }
}
