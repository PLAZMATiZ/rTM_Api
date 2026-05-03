using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rtm.Migrations
{
    /// <inheritdoc />
    public partial class AddTimer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "TaskItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "TaskItems",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "TaskItems");
        }
    }
}
