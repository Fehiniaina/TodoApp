using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskActivitySequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_task_activities_task_id_occurred_at_utc",
                table: "task_activities");

            migrationBuilder.AddColumn<long>(
                name: "sequence",
                table: "task_activities",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateIndex(
                name: "ix_task_activities_task_id_occurred_at_utc_sequence",
                table: "task_activities",
                columns: new[] { "task_id", "occurred_at_utc", "sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_task_activities_task_id_occurred_at_utc_sequence",
                table: "task_activities");

            migrationBuilder.DropColumn(
                name: "sequence",
                table: "task_activities");

            migrationBuilder.CreateIndex(
                name: "ix_task_activities_task_id_occurred_at_utc",
                table: "task_activities",
                columns: new[] { "task_id", "occurred_at_utc" });
        }
    }
}
