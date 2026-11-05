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
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyList<TagDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var tags = await cache.GetOrCreateAsync(TagCache.AllTagsKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            await using var db = await factory.CreateDbContextAsync(cancellationToken);

            return await db.Tags
                .TagWith("GetAllTags")
                .OrderBy(t => t.Name)
                .Select(t => new TagDto(t.Id, t.Name, t.Color))
                .ToListAsync(cancellationToken);
        });

        return tags ?? [];
    }
}
