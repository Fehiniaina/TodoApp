using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsCompletedWithStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_completed",
                table: "tasks");

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "tasks");

            migrationBuilder.AddColumn<bool>(
                name: "is_completed",
                table: "tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
