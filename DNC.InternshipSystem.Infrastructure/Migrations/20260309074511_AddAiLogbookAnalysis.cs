using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DNC.InternshipSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiLogbookAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiScore",
                table: "Logbooks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSuggestions",
                table: "Logbooks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiScore",
                table: "Logbooks");

            migrationBuilder.DropColumn(
                name: "AiSuggestions",
                table: "Logbooks");
        }
    }
}
