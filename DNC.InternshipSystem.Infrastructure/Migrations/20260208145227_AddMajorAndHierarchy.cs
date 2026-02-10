using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DNC.InternshipSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMajorAndHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Departments_DepartmentId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Classes_ClassId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Major",
                table: "Students");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "Classes",
                newName: "MajorId");

            migrationBuilder.RenameIndex(
                name: "IX_Classes_DepartmentId",
                table: "Classes",
                newName: "IX_Classes_MajorId");

            migrationBuilder.AlterColumn<string>(
                name: "ClassId",
                table: "Students",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "GraduationYear",
                table: "StudentBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Classes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Majors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Majors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Majors_StudentBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "StudentBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Majors",
                columns: new[] { "Id", "BatchId", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, 1, "CNPM", true, "Công nghệ Phần mềm" },
                    { 2, 1, "CNTT", true, "Công nghệ Thông tin" },
                    { 3, 1, "KHMT", true, "Khoa học Máy tính" },
                    { 4, 2, "CNPM", true, "Công nghệ Phần mềm" },
                    { 5, 2, "CNTT", true, "Công nghệ Thông tin" },
                    { 6, 3, "CNPM", true, "Công nghệ Phần mềm" },
                    { 7, 3, "CNTT", true, "Công nghệ Thông tin" },
                    { 8, 3, "KHMT", true, "Khoa học Máy tính" }
                });

            migrationBuilder.UpdateData(
                table: "StudentBatches",
                keyColumn: "Id",
                keyValue: 1,
                column: "GraduationYear",
                value: 2025);

            migrationBuilder.UpdateData(
                table: "StudentBatches",
                keyColumn: "Id",
                keyValue: 2,
                column: "GraduationYear",
                value: 2026);

            migrationBuilder.UpdateData(
                table: "StudentBatches",
                keyColumn: "Id",
                keyValue: 3,
                column: "GraduationYear",
                value: 2027);

            migrationBuilder.CreateIndex(
                name: "IX_Majors_BatchId",
                table: "Majors",
                column: "BatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Majors_MajorId",
                table: "Classes",
                column: "MajorId",
                principalTable: "Majors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Classes_ClassId",
                table: "Students",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Majors_MajorId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Classes_ClassId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "Majors");

            migrationBuilder.DropColumn(
                name: "GraduationYear",
                table: "StudentBatches");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Classes");

            migrationBuilder.RenameColumn(
                name: "MajorId",
                table: "Classes",
                newName: "DepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Classes_MajorId",
                table: "Classes",
                newName: "IX_Classes_DepartmentId");

            migrationBuilder.AlterColumn<string>(
                name: "ClassId",
                table: "Students",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Major",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Departments_DepartmentId",
                table: "Classes",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Classes_ClassId",
                table: "Students",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
