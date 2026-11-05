using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Time.Testing;
using TodoApp.Domain.Tags;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Tags.GetAllTags;
using TodoApp.Features.Tags.Shared;
using TodoApp.Features.Tasks.Tags.AddTagToTask;
using TodoApp.Infrastructure.Persistence;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Features.Tags;

[Collection("Postgres")]
public sealed class TagCacheTests(PostgresFixture fixture) : IAsyncLifetime, IDisposable
{
    private const string Owner = "owner-A";
    private static readonly DateTime Now = new(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc);

    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(Now));
    private TagCacheInvalidationInterceptor Interceptor => new(_cache);

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(Owner);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _cache.Dispose();

    [Fact]
    public async Task Tag_created_through_the_application_appears_immediately()
    {
        await SeedTagsAsync("Backend");
        Assert.Equal(["Backend"], await AllTagNamesAsync()); // cache rempli

        // Le vrai chemin de l'application : ajouter un tag inconnu à une tâche le crée.
        var taskId = await SeedTaskAsync();
        var handler = new AddTagToTaskHandler(
            new InterceptedDbContextFactory(fixture, Interceptor),
            new AddTagToTaskCommandValidator(),
            new FakeCurrentUser(Owner),
            _time,
            new TaskChangeNotifier());

        Assert.True((await handler.HandleAsync(new AddTagToTaskCommand(taskId, "Frontend"))).IsSuccess);

        Assert.Equal(["Backend", "Frontend"], await AllTagNamesAsync());
    }

    [Fact]
    public async Task Renamed_tag_appears_immediately()
    {
        await SeedTagsAsync("Frontend");
        Assert.Equal(["Frontend"], await AllTagNamesAsync());

        await using (var db = fixture.CreateDbContext(Interceptor))
        {
            var tag = await db.Tags.AsTracking().SingleAsync();
            tag.Rename("Frontend web");
            await db.SaveChangesAsync();
        }

        Assert.Equal(["Frontend web"], await AllTagNamesAsync());
    }

    [Fact]
    public async Task Deleted_tag_disappears_immediately()
    {
        await SeedTagsAsync("Backend", "Frontend");
        Assert.Equal(["Backend", "Frontend"], await AllTagNamesAsync());

        await using (var db = fixture.CreateDbContext(Interceptor))
        {
            db.Tags.Remove(await db.Tags.AsTracking().SingleAsync(t => t.Name == "Frontend"));
            await db.SaveChangesAsync();
        }

        Assert.Equal(["Backend"], await AllTagNamesAsync());
    }

    [Fact]
    public async Task Failed_save_keeps_the_cache()
    {
        await SeedTagsAsync("Frontend");
        Assert.Equal(["Frontend"], await AllTagNamesAsync());

        // Doublon : l'index unique refuse l'écriture, rien n'a changé en base.
        await using (var db = fixture.CreateDbContext(Interceptor))
        {
            db.Tags.Add(Tag.Create("FRONTEND", TagColor.Red, Owner, Now));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        Assert.True(_cache.TryGetValue(TagCache.AllTagsKey, out _));
    }

    [Fact]
    public async Task Changes_to_other_entities_keep_the_cache()
    {
        await SeedTagsAsync("Frontend");
        Assert.Equal(["Frontend"], await AllTagNamesAsync());

        await using (var db = fixture.CreateDbContext(Interceptor))
        {
            db.Tasks.Add(TaskItem.Create(Owner, "Sans rapport", null, TaskPriority.Medium, null, Now, TaskType.Task));
            await db.SaveChangesAsync();
        }

        Assert.True(_cache.TryGetValue(TagCache.AllTagsKey, out _));
    }

    private async Task<IReadOnlyList<string>> AllTagNamesAsync()
    {
        var handler = new GetAllTagsHandler(new TestDbContextFactory(fixture), _cache);
        return [.. (await handler.HandleAsync()).Select(t => t.Name)];
    }

    private async Task SeedTagsAsync(params string[] names)
    {
        await using var db = fixture.CreateDbContext();
        db.Tags.AddRange(names.Select(n => Tag.Create(n, TagColor.Blue, Owner, Now)));
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedTaskAsync()
    {
        var task = TaskItem.Create(Owner, "Tâche", null, TaskPriority.Medium, null, Now, TaskType.Task);

        await using var db = fixture.CreateDbContext();
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task.Id;
    }
}
