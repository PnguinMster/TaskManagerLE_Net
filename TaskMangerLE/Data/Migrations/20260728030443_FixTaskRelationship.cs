using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskMangerLE.Migrations
{
    /// <inheritdoc />
    public partial class FixTaskRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_ProjectItems_ProjectItemId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ProjectItemId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "ProjectItemId",
                table: "TaskItems");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ProjectId",
                table: "TaskItems",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_ProjectItems_ProjectId",
                table: "TaskItems",
                column: "ProjectId",
                principalTable: "ProjectItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_ProjectItems_ProjectId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ProjectId",
                table: "TaskItems");

            migrationBuilder.AddColumn<int>(
                name: "ProjectItemId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ProjectItemId",
                table: "TaskItems",
                column: "ProjectItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_ProjectItems_ProjectItemId",
                table: "TaskItems",
                column: "ProjectItemId",
                principalTable: "ProjectItems",
                principalColumn: "Id");
        }
    }
}
