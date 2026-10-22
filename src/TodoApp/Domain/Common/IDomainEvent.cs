namespace TodoApp.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
