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
public sealed class TagCacheTests : IAsyncLifetime, IDisposable
{
    private const string Owner = "owner-A";
    private static readonly DateTime Now = new(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc);

    private readonly PostgresFixture _fixture;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly TagCache _tagCache;
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(Now));
    private TagCacheInvalidationInterceptor Interceptor => new(_tagCache);

    public TagCacheTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _tagCache = new TagCache(_cache);
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await _fixture.CreateUsersAsync(Owner);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        _tagCache.Dispose();
        _cache.Dispose();
    }

    [Fact]
    public async Task Tag_created_through_the_application_appears_immediately()
    {
        await SeedTagsAsync("Backend");
        Assert.Equal(["Backend"], await AllTagNamesAsync()); // cache rempli

        // Le vrai chemin de l'application : ajouter un tag inconnu à une tâche le crée.
        var taskId = await SeedTaskAsync();
        var handler = new AddTagToTaskHandler(
            new InterceptedDbContextFactory(_fixture, Interceptor),
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

        await using (var db = _fixture.CreateDbContext(Interceptor))
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

        await using (var db = _fixture.CreateDbContext(Interceptor))
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
        await using (var db = _fixture.CreateDbContext(Interceptor))
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

        await using (var db = _fixture.CreateDbContext(Interceptor))
        {
            db.Tasks.Add(TaskItem.Create(Owner, "Sans rapport", null, TaskPriority.Medium, null, Now, TaskType.Task));
            await db.SaveChangesAsync();
        }

        Assert.True(_cache.TryGetValue(TagCache.AllTagsKey, out _));
    }

    [Fact]
    public async Task Concurrent_requests_on_an_empty_cache_query_the_database_once()
    {
        await SeedTagsAsync("Backend", "Frontend");

        var counter = new CommandCounter();
        var handler = new GetAllTagsHandler(new InterceptedDbContextFactory(_fixture, counter), _tagCache);

        // 50 utilisateurs ouvrent la liste au même instant, alors que le cache est vide.
        var results = await Task.WhenAll(
            Enumerable.Range(0, 50).Select(_ => Task.Run(() => handler.HandleAsync())));

        Assert.Equal(1, counter.Count);
        Assert.All(results, r => Assert.Equal(["Backend", "Frontend"], r.Select(t => t.Name)));
    }

    [Fact]
    public async Task Load_overlapping_an_invalidation_does_not_cache_stale_tags()
    {
        await SeedTagsAsync("Frontend");

        var gate = new PauseAfterQuery("GetAllTags");
        var handler = new GetAllTagsHandler(new InterceptedDbContextFactory(_fixture, gate), _tagCache);

        // 1. A lit la base, puis s'arrête avant d'écrire dans le cache.
        var loading = handler.HandleAsync();
        await gate.Reached.WaitAsync(TimeSpan.FromSeconds(10));

        // 2. B renomme le tag pendant ce temps ; l'intercepteur invalide.
        await using (var db = _fixture.CreateDbContext(Interceptor))
        {
            var tag = await db.Tags.AsTracking().SingleAsync();
            tag.Rename("Frontend web");
            await db.SaveChangesAsync();
        }

        // 3. A termine.
        gate.Release();
        var loadedByA = await loading;

        // A a commencé sa lecture avant le renommage : l'ancien nom est correct POUR LUI…
        Assert.Equal(["Frontend"], loadedByA.Select(t => t.Name));

        // … mais ce résultat ne doit pas avoir été mis en cache pour les autres.
        Assert.Equal(["Frontend web"], await AllTagNamesAsync());
    }

    private async Task<IReadOnlyList<string>> AllTagNamesAsync()
    {
        var handler = new GetAllTagsHandler(new TestDbContextFactory(_fixture), _tagCache);
        return [.. (await handler.HandleAsync()).Select(t => t.Name)];
    }

    private async Task SeedTagsAsync(params string[] names)
    {
        await using var db = _fixture.CreateDbContext();
        db.Tags.AddRange(names.Select(n => Tag.Create(n, TagColor.Blue, Owner, Now)));
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedTaskAsync()
    {
        var task = TaskItem.Create(Owner, "Tâche", null, TaskPriority.Medium, null, Now, TaskType.Task);

        await using var db = _fixture.CreateDbContext();
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task.Id;
    }
}
