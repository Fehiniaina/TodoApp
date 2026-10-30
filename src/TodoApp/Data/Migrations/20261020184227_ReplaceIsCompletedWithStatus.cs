using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsCompletedWithStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. EXPAND : ajouter la nouvelle colonne, l'ancienne reste en place.
            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0); // 0 = ToDo : correct pour les tâches non terminées

            // 2. BACKFILL : convertir l'information AVANT de détruire sa source.
            // Valeurs en dur (migration figée) : 3 = Resolved, le seul statut « terminé » du workflow.
            migrationBuilder.Sql("UPDATE tasks SET status = 3 WHERE is_completed = true;");

            // 3. CONTRACT : supprimer l'ancienne colonne seulement maintenant.
            migrationBuilder.DropColumn(
                name: "is_completed",
                table: "tasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Même ordre, à l'envers : on recrée l'ancienne colonne, on la remplit, puis on supprime la nouvelle.
            migrationBuilder.AddColumn<bool>(
                name: "is_completed",
                table: "tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Tout statut « terminé » redevient is_completed = true (3 = Resolved, 4 = Done).
            migrationBuilder.Sql("UPDATE tasks SET is_completed = true WHERE status IN (3, 4);");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tasks");
        }
    }
}
