using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Tags.Shared;

namespace TodoApp.Features.Tags.GetAllTags;

/// <summary>Tous les tags, triés par nom. Mis en cache : la liste change rarement.</summary>
internal sealed class GetAllTagsHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    IMemoryCache cache) : IHandler
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public async Task<ImmutableArray<TagDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(TagCache.AllTagsKey, out ImmutableArray<TagDto> cachedTags))
        {
            return cachedTags;
        }

        await Lock.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue(TagCache.AllTagsKey, out ImmutableArray<TagDto> cacheData))
            {
                return cacheData;
            }

            await using var db = await factory.CreateDbContextAsync(cancellationToken);

            var tags = await db.Tags
                .TagWith("GetAllTags")
                .OrderBy(t => t.Name)
                .Select(t => new TagDto(t.Id, t.Name, t.Color))
                .ToListAsync(cancellationToken);

            var snapshot = tags.ToImmutableArray();
            cache.Set(TagCache.AllTagsKey, snapshot, CacheDuration);

            return snapshot;
        }
        finally
        {
            Lock.Release();
        }
    }
}
