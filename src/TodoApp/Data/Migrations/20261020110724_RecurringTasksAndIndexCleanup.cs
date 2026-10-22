using System;
using Microsoft.EntityFrameworkCore.Migrations;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Recurrence;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RecurringTasksAndIndexCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tasks_id",
                table: "tasks");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:recurrence_frequency", "daily,weekly,monthly")
                .Annotation("Npgsql:Enum:task_priority", "high,low,medium,urgent")
                .OldAnnotation("Npgsql:Enum:task_priority", "high,low,medium,urgent");

            migrationBuilder.AddColumn<Guid>(
                name: "recurring_task_id",
                table: "tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recurring_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    priority = table.Column<TaskPriority>(type: "task_priority", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    next_occurrence = table.Column<DateOnly>(type: "date", nullable: true),
                    is_stopped = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    rule_day_of_month = table.Column<int>(type: "integer", nullable: true),
                    rule_days_of_week = table.Column<int>(type: "integer", nullable: false),
                    rule_frequency = table.Column<RecurrenceFrequency>(type: "recurrence_frequency", nullable: false),
                    rule_interval = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recurring_tasks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_recurring_task_id_due_date",
                table: "tasks",
                columns: new[] { "recurring_task_id", "due_date" },
                unique: true,
                filter: "recurring_task_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_recurring_tasks_next_occurrence",
                table: "recurring_tasks",
                column: "next_occurrence",
                filter: "is_stopped = false");

            migrationBuilder.CreateIndex(
                name: "ix_recurring_tasks_owner_id",
                table: "recurring_tasks",
                column: "owner_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tasks_recurring_tasks_recurring_task_id",
                table: "tasks",
                column: "recurring_task_id",
                principalTable: "recurring_tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tasks_recurring_tasks_recurring_task_id",
                table: "tasks");

            migrationBuilder.DropTable(
                name: "recurring_tasks");

            migrationBuilder.DropIndex(
                name: "ix_tasks_recurring_task_id_due_date",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "recurring_task_id",
                table: "tasks");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:task_priority", "high,low,medium,urgent")
                .OldAnnotation("Npgsql:Enum:recurrence_frequency", "daily,weekly,monthly")
                .OldAnnotation("Npgsql:Enum:task_priority", "high,low,medium,urgent");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_id",
                table: "tasks",
                column: "id",
                unique: true);
        }
    }
}
