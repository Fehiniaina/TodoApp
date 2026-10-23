using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TodoApp.Domain.Notifications;
using TodoApp.Features.Notifications.Realtime;

namespace TodoApp.Infrastructure.Persistence;

internal sealed class NotificationPublishingInterceptor(INotificationBroadcaster broadcaster)
    : SaveChangesInterceptor
{
    // Un même intercepteur (singleton) sert tous les DbContext : on garde les destinataires par contexte.
    private readonly ConditionalWeakTable<DbContext, string[]> _pending = [];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
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
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Flush(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        Discard(eventData.Context);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Discard(eventData.Context);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // Added : nouvelle notification. Modified : lue dans un autre onglet → badge à resynchroniser.
        var recipients = context.ChangeTracker
            .Entries<Notification>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e => e.Entity.RecipientId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (recipients.Length > 0)
        {
            _pending.AddOrUpdate(context, recipients);
        }
    }

    private void Flush(DbContext? context)
    {
        if (context is null || !_pending.TryGetValue(context, out var recipients))
        {
            return;
        }

        _pending.Remove(context);

        foreach (var recipientId in recipients)
        {
            broadcaster.Publish(recipientId);
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
