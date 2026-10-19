using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tasks_owner_id_priority",
                table: "tasks");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_tasks_owner_id_deleted_at",
                table: "tasks",
                columns: new[] { "owner_id", "deleted_at" },
                filter: "is_deleted = true");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_owner_id_priority",
                table: "tasks",
                columns: new[] { "owner_id", "priority" },
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tasks_owner_id_deleted_at",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "ix_tasks_owner_id_priority",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "tasks");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_owner_id_priority",
                table: "tasks",
                columns: new[] { "owner_id", "priority" });
        }
    }
}
