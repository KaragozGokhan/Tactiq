using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TactiqAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_CreatedByUserId",
                table: "Matches",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_CreatedByUserId",
                table: "Matches",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_CreatedByUserId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_CreatedByUserId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Matches");
        }
    }
}
