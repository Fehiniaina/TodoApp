using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TodoApp.Domain.Tags;
using TodoApp.Features.Tags.Shared;

namespace TodoApp.Infrastructure.Persistence;

/// <summary>
/// Vide le cache des tags dès qu'un Tag est ajouté, modifié ou supprimé via EF : aucun handler
/// n'a à y penser (leçon du piège de propagation).
/// <para>
/// Invalidation APRÈS l'écriture (SavedChanges) : vider avant laisserait une lecture concurrente
/// remettre l'ancien état en cache pour toute la durée d'expiration.
/// </para>
/// Limite : ExecuteUpdate/ExecuteDelete et le SQL brut contournent le suivi des entités ;
/// l'expiration de <see cref="TagCache"/> sert de filet de sécurité.
/// </summary>
internal sealed class TagCacheInvalidationInterceptor(TagCache cache) : SaveChangesInterceptor
{
    private static readonly object Marker = new();

    // L'intercepteur est un singleton partagé par tous les contextes : l'état « un tag a changé »
    // est mémorisé PAR contexte, sans empêcher le ramasse-miettes de les libérer.
    private readonly ConditionalWeakTable<DbContext, object> _pending = new();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Track(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Track(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Invalidate(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Invalidate(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        Forget(eventData.Context);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Forget(eventData.Context);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Track(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var tagsChanged = context.ChangeTracker.Entries<Tag>()
            .Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        if (tagsChanged)
        {
            _pending.AddOrUpdate(context, Marker);
        }
    }

    /// <summary>Écriture réussie : le cache ne reflète plus la base.</summary>
    private void Invalidate(DbContext? context)
    {
        if (context is not null && _pending.Remove(context))
        {
            cache.Invalidate();
        }
    }

    /// <summary>Écriture échouée : rien n'a changé en base, le cache reste valable.</summary>
    private void Forget(DbContext? context)
    {
        if (context is not null)
        {
            _pending.Remove(context);
        }
    }
}
