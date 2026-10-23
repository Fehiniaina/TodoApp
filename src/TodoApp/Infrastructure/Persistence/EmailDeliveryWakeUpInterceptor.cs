using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TodoApp.Domain.Notifications;
using TodoApp.Features.Notifications.Email;

namespace TodoApp.Infrastructure.Persistence;

/// <summary>Après un commit qui a créé des emails à envoyer, réveille le job d'envoi.</summary>
internal sealed class EmailDeliveryWakeUpInterceptor(EmailDeliverySignal signal) : SaveChangesInterceptor
{
    private static readonly object Marker = new();
    private readonly ConditionalWeakTable<DbContext, object> _pending = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
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

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Flush(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Flush(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        Discard(eventData.Context);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Discard(eventData.Context);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (context is not null
            && context.ChangeTracker.Entries<EmailDelivery>().Any(e => e.State == EntityState.Added))
        {
            _pending.AddOrUpdate(context, Marker);
        }
    }

    private void Flush(DbContext? context)
    {
        if (context is not null && _pending.Remove(context))
        {
            signal.Notify();
        }
    }

    private void Discard(DbContext? context)
    {
        if (context is not null)
        {
            _pending.Remove(context);
        }
    }
}
