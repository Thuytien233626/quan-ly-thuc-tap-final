using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DNC.InternshipSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIsExternal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExternal",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExternal",
                table: "Companies");
        }
    }
}
