namespace TodoApp.Domain.Tasks.Exceptions;

internal sealed class InvalidStatusTransitionException : InvalidOperationException
{
    public InvalidStatusTransitionException()
    {
    }

    public InvalidStatusTransitionException(string message)
        : base(message)
    {
    }

    public InvalidStatusTransitionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public InvalidStatusTransitionException(TaskItemStatus from, TaskItemStatus to)
        : base($"A task cannot move from {from} to {to}.")
    {
        From = from;
        To = to;
    }

    /// <summary>Statut de départ (renseigné par le constructeur métier).</summary>
    public TaskItemStatus? From { get; }

    /// <summary>Statut demandé (renseigné par le constructeur métier).</summary>
    public TaskItemStatus? To { get; }
}
