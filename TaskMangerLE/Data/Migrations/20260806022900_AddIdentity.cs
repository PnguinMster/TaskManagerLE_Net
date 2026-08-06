using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskMangerLE.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "ProjectItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectItems_OwnerId",
                table: "ProjectItems",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectItems_AspNetUsers_OwnerId",
                table: "ProjectItems",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectItems_AspNetUsers_OwnerId",
                table: "ProjectItems");

            migrationBuilder.DropIndex(
                name: "IX_ProjectItems_OwnerId",
                table: "ProjectItems");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ProjectItems");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AspNetUsers");
        }
    }
}
