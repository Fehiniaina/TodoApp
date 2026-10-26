using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TodoApp.Features.Tasks.Attachments.Cleanup;
using TodoApp.Infrastructure.Storage;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Features.Tasks.Attachments;

[Collection("Postgres")]
public class OrphanFileCleanerTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Alice = "alice";

    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(1);

    // Horloge réelle au départ : les dates des fichiers viennent du système de fichiers.
    private readonly FakeTimeProvider _time = new(DateTimeOffset.UtcNow);

    private readonly string _root = Path.Combine(Path.GetTempPath(), "todoapp-tests", Guid.NewGuid().ToString("N"));

    private TaskScenario Scenario => new(fixture, _time);

    private LocalFileStorage Storage => new(Options.Create(new FileStorageOptions { RootPath = _root }), _time);

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(Alice);
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Old_orphan_file_is_deleted()
    {
        var orphan = await SaveFileAsync();
        _time.Advance(GracePeriod + TimeSpan.FromMinutes(1));

        var deleted = await Cleaner().RunOnceAsync(CancellationToken.None);

        Assert.Equal(1, deleted);
        Assert.False(await FileExistsAsync(orphan));
    }

    [Fact]
    public async Task Recent_orphan_file_is_kept_during_the_grace_period()
    {
        var orphan = await SaveFileAsync();
        _time.Advance(TimeSpan.FromMinutes(5));

        var deleted = await Cleaner().RunOnceAsync(CancellationToken.None);

        Assert.Equal(0, deleted);
        Assert.True(await FileExistsAsync(orphan));
    }

    [Fact]
    public async Task Referenced_file_is_kept()
    {
        var taskId = await Scenario.CreateTaskAsync(Alice);
        var key = await AttachFileAsync(taskId);
        _time.Advance(GracePeriod + TimeSpan.FromMinutes(1));

        await Cleaner().RunOnceAsync(CancellationToken.None);

        Assert.True(await FileExistsAsync(key));
    }

    [Fact]
    public async Task File_of_a_trashed_task_is_kept_because_the_task_can_be_restored()
    {
        var taskId = await Scenario.CreateTaskAsync(Alice);
        var key = await AttachFileAsync(taskId);
        await Scenario.TrashAsync(Alice, taskId);
        _time.Advance(GracePeriod + TimeSpan.FromMinutes(1));

        await Cleaner().RunOnceAsync(CancellationToken.None);

        Assert.True(await FileExistsAsync(key));
    }

    [Fact]
    public async Task File_of_a_purged_task_is_deleted()
    {
        var taskId = await Scenario.CreateTaskAsync(Alice);
        var key = await AttachFileAsync(taskId);

        // Ce que fait la purge de la corbeille : suppression en masse, cascade en base.
        await using (var db = fixture.CreateDbContext())
        {
            await db.Tasks.IgnoreQueryFilters().Where(t => t.Id == taskId).ExecuteDeleteAsync();
        }

        _time.Advance(GracePeriod + TimeSpan.FromMinutes(1));

        var deleted = await Cleaner().RunOnceAsync(CancellationToken.None);

        Assert.Equal(1, deleted);
        Assert.Null(await Storage.OpenReadAsync(key, CancellationToken.None));
    }

    private OrphanFileCleaner Cleaner() => new(
        new TestDbContextFactory(fixture),
        Storage,
        _time,
        Options.Create(new OrphanFileCleanupOptions { GracePeriod = GracePeriod }),
        NullLogger<OrphanFileCleaner>.Instance);

    private Task<string> SaveFileAsync() =>
        Storage.SaveAsync(new MemoryStream("%PDF-1.7"u8.ToArray()), CancellationToken.None);

    private async Task<string> AttachFileAsync(Guid taskId)
    {
        var key = await SaveFileAsync();

        await using var db = fixture.CreateDbContext();
        var task = await db.Tasks.AsTracking().Include(t => t.Attachments).SingleAsync(t => t.Id == taskId);
        task.AddAttachment("devis.pdf", "application/pdf", 8, key, Alice, _time.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync();

        return key;
    }

    private async Task<bool> FileExistsAsync(string key)
    {
        await using var stream = await Storage.OpenReadAsync(key, CancellationToken.None);
        return stream is not null;
    }
}
