namespace TodoApp.Domain.Tasks;

internal static class TaskDueDate
{
    /// <summary>
    /// "today" est la date du jour DANS LE FUSEAU DE L'UTILISATEUR :
    /// le domaine ne connaît ni l'horloge ni les fuseaux.
    /// </summary>
    public static DueStatus Evaluate(DateOnly? dueDate, bool isCompleted, DateOnly today) =>
    (dueDate, isCompleted) switch
    {
        (null, _) or (_, true) => DueStatus.None,
        ({ } due, _) when due < today => DueStatus.Overdue,
        ({ } due, _) when due == today => DueStatus.DueToday,
        _ => DueStatus.Upcoming,
    };
}