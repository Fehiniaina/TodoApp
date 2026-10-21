using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskAssignee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "assignee_id",
                table: "tasks",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tasks_assignee_id",
                table: "tasks",
                column: "assignee_id",
                filter: "is_deleted = false AND assignee_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_tasks_asp_net_users_assignee_id",
                table: "tasks",
                column: "assignee_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tasks_asp_net_users_assignee_id",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "ix_tasks_assignee_id",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "assignee_id",
                table: "tasks");
        }
    }
}
