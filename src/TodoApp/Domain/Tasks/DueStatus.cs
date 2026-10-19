namespace TodoApp.Domain.Tasks;

internal enum DueStatus
{
    None,       // pas d'échéance, ou tâche terminée
    Upcoming,
    DueToday,
    Overdue,
}