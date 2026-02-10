using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DNC.InternshipSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLecturerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxStudents",
                table: "Lecturers");

            migrationBuilder.AddColumn<string>(
                name: "LecturerCode",
                table: "Lecturers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "Lecturers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LecturerId",
                table: "Classes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_LecturerId",
                table: "Classes",
                column: "LecturerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Lecturers_LecturerId",
                table: "Classes",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Lecturers_LecturerId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_LecturerId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "LecturerCode",
                table: "Lecturers");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Lecturers");

            migrationBuilder.DropColumn(
                name: "LecturerId",
                table: "Classes");

            migrationBuilder.AddColumn<int>(
                name: "MaxStudents",
                table: "Lecturers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
