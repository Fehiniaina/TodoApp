using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Tags.Shared;

namespace TodoApp.Features.Tags.GetAllTags;

/// <summary>
/// Tous les tags, triés par nom. Mis en cache, invalidé par <c>TagCacheInvalidationInterceptor</c>.
/// <para>
/// Protection contre la ruée : quand le cache est vide (démarrage, expiration, invalidation),
/// un seul appel interroge la base ; les autres attendent puis lisent son résultat.
/// </para>
/// Limites connues : un verrou unique (une seule clé), propre à chaque instance de l'application.
/// Passer à HybridCache si l'on ajoute d'autres clés ou un cache distribué.
/// </summary>
internal sealed class GetAllTagsHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    TagCache cache) : IHandler
{
    public Task<ImmutableArray<TagDto>> HandleAsync(CancellationToken cancellationToken = default) =>
        cache.GetOrLoadAsync(LoadAsync, cancellationToken);

    private async Task<ImmutableArray<TagDto>> LoadAsync(CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var tags = await db.Tags
            .TagWith("GetAllTags")
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Color))
            .ToListAsync(cancellationToken);

        return [.. tags];
    }
}
