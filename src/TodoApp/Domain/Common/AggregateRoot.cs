namespace TodoApp.Domain.Common;

internal abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Événements en attente, convertis en historique au moment de SaveChanges.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
