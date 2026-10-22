using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameOwnerIdToCreatedById : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "owner_id",
                table: "tasks",
                newName: "created_by_id");

            migrationBuilder.RenameIndex(
                name: "ix_tasks_owner_id_priority",
                table: "tasks",
                newName: "ix_tasks_created_by_id_priority");

            migrationBuilder.RenameIndex(
                name: "ix_tasks_owner_id_deleted_at",
                table: "tasks",
                newName: "ix_tasks_created_by_id_deleted_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "created_by_id",
                table: "tasks",
                newName: "owner_id");

            migrationBuilder.RenameIndex(
                name: "ix_tasks_created_by_id_priority",
                table: "tasks",
                newName: "ix_tasks_owner_id_priority");

            migrationBuilder.RenameIndex(
                name: "ix_tasks_created_by_id_deleted_at",
                table: "tasks",
                newName: "ix_tasks_owner_id_deleted_at");
        }
    }
}
