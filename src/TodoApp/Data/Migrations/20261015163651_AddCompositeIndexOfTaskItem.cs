using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexOfTaskItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tasks_owner_id",
                table: "tasks");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_id",
                table: "tasks",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tasks_owner_id_priority",
                table: "tasks",
                columns: new[] { "owner_id", "priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tasks_id",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "ix_tasks_owner_id_priority",
                table: "tasks");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_owner_id",
                table: "tasks",
                column: "owner_id");
        }
    }
}
