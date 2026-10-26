using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskMangerLE.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedNameAndLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAttUtc",
                table: "TaskItems",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAttUtc",
                table: "ProjectItems",
                newName: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "TaskItems",
                newName: "UpdatedAttUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "ProjectItems",
                newName: "UpdatedAttUtc");
        }
    }
}
