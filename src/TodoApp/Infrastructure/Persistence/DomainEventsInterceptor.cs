using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TodoApp.Domain.Common;
using TodoApp.Domain.Notifications;
using TodoApp.Domain.Tasks.Activity;

namespace TodoApp.Infrastructure.Persistence;

/// <summary>
/// Juste avant SaveChanges : convertit les événements de domaine en lignes d'historique.
/// Données et historique partent dans la même transaction : jamais l'un sans l'autre.
/// </summary>
internal sealed class DomainEventsInterceptor : SaveChangesInterceptor
{
    public static readonly DomainEventsInterceptor Instance = new();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Capture(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                if (TaskActivityMapper.ToActivity(domainEvent) is { } activity)
                {
                    context.Add(activity);
                }

                if (NotificationMapper.ToNotification(domainEvent) is { } notification)
                {
                    context.Add(notification);
                }
            }

            aggregate.ClearDomainEvents();
        }
    }
}
