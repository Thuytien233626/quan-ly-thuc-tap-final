using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DNC.InternshipSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentBatchAndUpdateInternshipTerm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "InternshipTerms",
                newName: "RegistrationStart");

            migrationBuilder.RenameColumn(
                name: "RegistrationDeadline",
                table: "InternshipTerms",
                newName: "RegistrationEnd");

            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "InternshipTerms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "BatchId",
                table: "InternshipTerms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DurationInWeeks",
                table: "InternshipTerms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "InternshipStart",
                table: "InternshipTerms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Semester",
                table: "InternshipTerms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "InternshipTerms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StudentBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClassCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnrollmentYear = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentBatches", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "StudentBatches",
                columns: new[] { "Id", "BatchCode", "ClassCode", "EnrollmentYear", "IsActive" },
                values: new object[,]
                {
                    { 1, "K9", "DH21", 2021, true },
                    { 2, "K10", "DH22", 2022, true },
                    { 3, "K11", "DH23", 2023, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_InternshipTerms_BatchId",
                table: "InternshipTerms",
                column: "BatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_InternshipTerms_StudentBatches_BatchId",
                table: "InternshipTerms",
                column: "BatchId",
                principalTable: "StudentBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InternshipTerms_StudentBatches_BatchId",
                table: "InternshipTerms");

            migrationBuilder.DropTable(
                name: "StudentBatches");

            migrationBuilder.DropIndex(
                name: "IX_InternshipTerms_BatchId",
                table: "InternshipTerms");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "InternshipTerms");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "InternshipTerms");

            migrationBuilder.DropColumn(
                name: "DurationInWeeks",
                table: "InternshipTerms");

            migrationBuilder.DropColumn(
                name: "InternshipStart",
                table: "InternshipTerms");

            migrationBuilder.DropColumn(
                name: "Semester",
                table: "InternshipTerms");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "InternshipTerms");

            migrationBuilder.RenameColumn(
                name: "RegistrationStart",
                table: "InternshipTerms",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "RegistrationEnd",
                table: "InternshipTerms",
                newName: "RegistrationDeadline");
        }
    }
}
