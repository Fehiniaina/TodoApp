using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Shared;

internal static class TaskLoading
{
    /// <summary>
    /// Chargement d'une tâche destinée à être MODIFIÉE (entité suivie, agrégat complet).
    /// <para>
    /// Requêtes fractionnées : la checklist et les tags sont des collections possédées, chargées
    /// systématiquement avec la tâche. En une seule requête, leurs deux LEFT JOIN se multiplient
    /// (50 étapes × 10 tags = 500 lignes, chacune répétant toute la tâche). Fractionné : 1 + 50 + 10.
    /// </para>
    /// <para>
    /// Compromis accepté : les requêtes ne lisent pas un instantané unique. Une modification concurrente
    /// entre elles est très improbable et sans gravité ; si elle le devenait, la réponse serait un contrôle
    /// de concurrence optimiste (colonne de version), pas une transaction.
    /// </para>
    /// Les LECTURES (liste, détail) passent par des projections et ne sont pas concernées.
    /// </summary>
    public static IQueryable<TaskItem> ForUpdate(this IQueryable<TaskItem> tasks) =>
        tasks.AsTracking().AsSplitQuery();
}
