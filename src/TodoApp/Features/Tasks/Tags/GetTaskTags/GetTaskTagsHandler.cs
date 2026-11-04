using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;
using TodoApp.Features.Tags.Shared;

namespace TodoApp.Features.Tasks.Tags.GetTaskTags;

/// <summary>Tags d'une tâche, triés par nom.</summary>
internal sealed class GetTaskTagsHandler(IDbContextFactory<ApplicationDbContext> factory) : IHandler
{
    public async Task<IReadOnlyList<TagDto>> HandleAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var tagIds = db.Tasks
            .Where(t => t.Id == taskId)
            .SelectMany(t => t.Tags.Select(tt => tt.TagId));

        return await db.Tags
            .TagWith("GetTaskTags")
            .Where(tag => tagIds.Contains(tag.Id))
            .OrderBy(tag => tag.Name)
            .Select(tag => new TagDto(tag.Id, tag.Name, tag.Color))
            .ToListAsync(cancellationToken);
    }
}
