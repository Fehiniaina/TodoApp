using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations;

/// <inheritdoc />
public partial class StoreTaskStatusActivityAsNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Historique StatusChanged : nom du membre → valeur numérique (protégée par TaskItemStatusContractTests).
        // Liste FIGÉE : ne dépend pas de l'enum actuel. Valeur non reconnue laissée telle quelle.
        migrationBuilder.Sql("""
        UPDATE task_activities
        SET old_value = CASE old_value
                WHEN 'ToDo'         THEN '0'
                WHEN 'InProgress'   THEN '1'
                WHEN 'InValidation' THEN '2'
                WHEN 'Resolved'     THEN '3'
                WHEN 'Done'         THEN '4'
                ELSE old_value END,
            new_value = CASE new_value
                WHEN 'ToDo'         THEN '0'
                WHEN 'InProgress'   THEN '1'
                WHEN 'InValidation' THEN '2'
                WHEN 'Resolved'     THEN '3'
                WHEN 'Done'         THEN '4'
                ELSE new_value END
        WHERE type = 'StatusChanged';
        """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
        UPDATE task_activities
        SET old_value = CASE old_value
                WHEN '0' THEN 'ToDo' WHEN '1' THEN 'InProgress' WHEN '2' THEN 'InValidation'
                WHEN '3' THEN 'Resolved' WHEN '4' THEN 'Done' ELSE old_value END,
            new_value = CASE new_value
                WHEN '0' THEN 'ToDo' WHEN '1' THEN 'InProgress' WHEN '2' THEN 'InValidation'
                WHEN '3' THEN 'Resolved' WHEN '4' THEN 'Done' ELSE new_value END
        WHERE type = 'StatusChanged';
        """);
    }
}
