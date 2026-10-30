using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixTaskTypeDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Réparation de AddTaskType : toutes les lignes existantes ont reçu 0 (Bug) par défaut.
            // Sûr UNIQUEMENT parce qu'aucun utilisateur n'a encore pu choisir un type :
            // aucun 0 n'est un « vrai » Bug à ce stade.
            // Valeurs en dur volontairement : une migration est une photo figée,
            // elle ne doit pas dépendre de l'enum du domaine. 0 = Bug, 3 = Task.
            migrationBuilder.Sql("UPDATE tasks SET type = 3 WHERE type = 0;");

            migrationBuilder.AlterColumn<int>(
                name: "type",
                table: "tasks",
                type: "integer",
                nullable: false,
                defaultValue: 3,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "type",
                table: "tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 3);
        }
    }
}
