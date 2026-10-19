using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class _20261510_AddFirstAndLastName10_AddFirstAndLastName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "first_name", table: "AspNetUsers", type: "text", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_name", table: "AspNetUsers", type: "text", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "first_name", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "last_name", table: "AspNetUsers");
        }
    }
}
