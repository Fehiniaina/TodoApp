using Microsoft.EntityFrameworkCore.Migrations;
using TodoApp.Domain.Tasks;

#nullable disable

namespace TodoApp.Data.Migrations;

/// <inheritdoc />
public partial class MapTaskPriorityToPostgresEnum : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // (DropIndex sur id, si généré par EF : à conserver ici)

        // CREATE TYPE task_priority AS ENUM ('low', 'medium', 'high', 'urgent')
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:Enum:task_priority", "low,medium,high,urgent");

        // varchar 'Low' / 'Medium' / 'High' → enum 'low' / 'medium' / 'high'
        migrationBuilder.Sql("""
                ALTER TABLE tasks
                ALTER COLUMN priority TYPE task_priority
                USING lower(priority)::task_priority;
                """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // enum 'low' → varchar 'Low'
        migrationBuilder.Sql("""
                ALTER TABLE tasks
                ALTER COLUMN priority TYPE character varying(20)
                USING initcap(priority::text);
                """);

        // DROP TYPE task_priority
        migrationBuilder.AlterDatabase()
            .OldAnnotation("Npgsql:Enum:task_priority", "low,medium,high,urgent");

        // (CreateIndex sur id, si généré par EF : à conserver ici)
    }
}
