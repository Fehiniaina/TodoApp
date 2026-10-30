using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations;

/// <inheritdoc />
public partial class StoreTaskTypeActivityAsNumber : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Historique TypeChanged : nom du membre → valeur numérique (forme protégée par le test de contrat).
        // Liste FIGÉE, qui inclut les ANCIENS noms (« Extracte » avant le renommage en Extraction) :
        // une migration ne doit jamais dépendre de l'enum actuel.
        // Une valeur non reconnue est laissée telle quelle ; l'affichage la montre brute.
        migrationBuilder.Sql("""
        UPDATE task_activities
        SET old_value = CASE old_value
                WHEN 'Bug'        THEN '0'
                WHEN 'Extracte'   THEN '1'
                WHEN 'Extraction' THEN '1'
                WHEN 'Analyse'   THEN '2'
                WHEN 'Analyse'    THEN '2'
                WHEN 'Task'       THEN '3'
                WHEN 'Story'      THEN '4'
                ELSE old_value END,
            new_value = CASE new_value
                WHEN 'Bug'        THEN '0'
                WHEN 'Extracte'   THEN '1'
                WHEN 'Extraction' THEN '1'
                WHEN 'Analyse'   THEN '2'
                WHEN 'Analyse'    THEN '2'
                WHEN 'Task'       THEN '3'
                WHEN 'Story'      THEN '4'
                ELSE new_value END
        WHERE type = 'TypeChanged';
        """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Retour aux noms ACTUELS des membres (les anciens noms ne sont pas restaurés).
        migrationBuilder.Sql("""
        UPDATE task_activities
        SET old_value = CASE old_value
                WHEN '0' THEN 'Bug' WHEN '1' THEN 'Extraction' WHEN '2' THEN 'Analyse'
                WHEN '3' THEN 'Task' WHEN '4' THEN 'Story' ELSE old_value END,
            new_value = CASE new_value
                WHEN '0' THEN 'Bug' WHEN '1' THEN 'Extraction' WHEN '2' THEN 'Analyse'
                WHEN '3' THEN 'Task' WHEN '4' THEN 'Story' ELSE new_value END
        WHERE type = 'TypeChanged';
        """);
    }
}
