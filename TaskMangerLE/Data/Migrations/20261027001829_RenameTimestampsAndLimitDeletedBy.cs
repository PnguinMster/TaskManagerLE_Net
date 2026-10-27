using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskMangerLE.Migrations
{
    /// <inheritdoc />
    public partial class RenameTimestampsAndLimitDeletedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastUpdated",
                table: "TaskItems",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "TaskItems",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "LastUpdated",
                table: "ProjectItems",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "ProjectItems",
                newName: "CreatedAtUtc");

            migrationBuilder.AlterColumn<string>(
                name: "DeletedByUserId",
                table: "TaskItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeletedByUserId",
                table: "ProjectItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "TaskItems",
                newName: "LastUpdated");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "TaskItems",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "ProjectItems",
                newName: "LastUpdated");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ProjectItems",
                newName: "CreatedDate");

            migrationBuilder.AlterColumn<string>(
                name: "DeletedByUserId",
                table: "TaskItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeletedByUserId",
                table: "ProjectItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }
    }
}
