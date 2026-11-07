using System.Collections.Immutable;
using Microsoft.Extensions.Caching.Memory;

namespace TodoApp.Features.Tags.Shared;

/// <summary>
/// Cache de la liste des tags (singleton). Seul point d'accès à la clé, partagé par
/// GetAllTagsHandler (lecture) et TagCacheInvalidationInterceptor (invalidation).
/// <list type="bullet">
/// <item>Ruée : un seul chargement à la fois, avec double vérification.</item>
/// <item>Écriture périmée : chaque invalidation incrémente une génération ; un chargement
/// n'écrit son résultat que si aucune invalidation n'a eu lieu depuis son début.</item>
/// </list>
/// Limites : l'invalidation doit avoir lieu APRÈS la validation de la transaction (vrai avec
/// SaveChanges sans transaction explicite). ExecuteUpdate/Delete et le SQL brut ne sont pas
/// détectés : l'expiration sert de filet de sécurité. Un cache par instance de l'application.
/// </summary>
internal sealed class TagCache(IMemoryCache cache) : IDisposable
{
    public const string AllTagsKey = "tags:all";

    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);

    private readonly SemaphoreSlim _loadLock = new(1, 1);

    // Verrou très court, sans aucune entrée-sortie : protège la génération et rend
    // « comparer puis écrire » atomique face à « incrémenter puis vider ».
    private readonly Lock _gate = new();
    private long _generation;

    public async Task<ImmutableArray<TagDto>> GetOrLoadAsync(
        Func<CancellationToken, Task<ImmutableArray<TagDto>>> load,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(AllTagsKey, out ImmutableArray<TagDto> cached))
        {
            return cached;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue(AllTagsKey, out ImmutableArray<TagDto> loaded))
            {
                return loaded;
            }

            // Lue AVANT la requête : une invalidation survenue pendant la lecture sera détectée.
            var generation = CurrentGeneration();
            var snapshot = await load(cancellationToken);

            StoreIfStillCurrent(snapshot, generation);

            // Même non mis en cache, le résultat est correct pour cet appelant :
            // il reflète la base au moment où sa lecture a commencé.
            return snapshot;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>À appeler après la validation d'une écriture sur les tags.</summary>
    public void Invalidate()
    {
        lock (_gate)
        {
            _generation++;
            cache.Remove(AllTagsKey);
        }
    }

    public void Dispose() => _loadLock.Dispose();

    private long CurrentGeneration()
    {
        lock (_gate)
        {
            return _generation;
        }
    }

    private void StoreIfStillCurrent(ImmutableArray<TagDto> snapshot, long generation)
    {
        lock (_gate)
        {
            if (_generation == generation)
            {
                cache.Set(AllTagsKey, snapshot, Expiration);
            }
        }
    }
}
