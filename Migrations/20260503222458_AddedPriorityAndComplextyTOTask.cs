using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rtm.Migrations
{
    /// <inheritdoc />
    public partial class AddedPriorityAndComplextyTOTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Complexity",
                table: "TaskItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "TaskItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Complexity",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "TaskItems");
        }
    }
}
