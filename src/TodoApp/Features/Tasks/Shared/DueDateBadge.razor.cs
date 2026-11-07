using System.Globalization;
using Microsoft.AspNetCore.Components;
using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Shared;

public sealed partial class DueDateBadge
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    [Parameter] public DateOnly? DueDate { get; set; }
    [Parameter] public bool IsCompleted { get; set; }
    [Parameter, EditorRequired] public DateOnly Today { get; set; }

    private string Label(DateOnly due, DueStatus status) => status switch
    {
        DueStatus.Overdue => $"En retard · {DueDateFormat.Short(due, Today)}",
        DueStatus.DueToday => "Aujourd'hui",
        _ when due == Today.AddDays(1) => "Demain",
        _ => DueDateFormat.Short(due, Today),
    };

    /// <summary>
    /// Classe CSS explicite par statut : pas de ToString().ToLowerInvariant(),
    /// qui lierait le style au NOM du membre (un renommage ferait perdre la couleur).
    /// Exhaustivité vérifiée par DueDateBadgeTests.
    /// </summary>
    internal static string CssFor(DueStatus status) => status switch
    {
        DueStatus.None => "due-none",         // tâche terminée : style neutre
        DueStatus.Upcoming => "due-upcoming",
        DueStatus.DueToday => "due-duetoday",
        DueStatus.Overdue => "due-overdue",
        DueStatus.Later => "due-later",
        _ => "due-none",                      // ne doit jamais arriver : chaque membre a sa ligne ci-dessus
    };
}
