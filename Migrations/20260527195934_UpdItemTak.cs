using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rtm.Migrations
{
    /// <inheritdoc />
    public partial class UpdItemTak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaused",
                table: "TaskItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TotalSpentSeconds",
                table: "TaskItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaused",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "TotalSpentSeconds",
                table: "TaskItems");
        }
    }
}
