using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DNC.InternshipSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLogbookResubmitFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Submissions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ResubmitReason",
                table: "Logbooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ResubmitRequested",
                table: "Logbooks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResubmitReason",
                table: "Logbooks");

            migrationBuilder.DropColumn(
                name: "ResubmitRequested",
                table: "Logbooks");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Submissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
