using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TodoApp.Common;
using TodoApp.Domain.Tags;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Activity;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Tags.AddTagToTask;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Features.Tasks.Tags;

[Collection("Postgres")]
public sealed class AddTagToTaskHandlerTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string OwnerA = "owner-A";
    private const string OwnerB = "owner-B";
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 30, 9, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(OwnerA, OwnerB);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Unknown_name_creates_the_tag_and_adds_it()
    {
        var taskId = await CreateTaskAsync();

        var result = await Handler().HandleAsync(new AddTagToTaskCommand(taskId, "  front   end "));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.Equal("front end", result.Value.Name);

        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.Tags.CountAsync());
        var task = await db.Tasks.SingleAsync(t => t.Id == taskId);
        Assert.Equal(result.Value.Id, Assert.Single(task.Tags).TagId);
    }

    [Fact]
    public async Task Existing_name_is_reused_ignoring_case()
    {
        var first = await CreateTaskAsync();
        var second = await CreateTaskAsync();

        var a = await Handler().HandleAsync(new AddTagToTaskCommand(first, "Frontend"));
        var b = await Handler().HandleAsync(new AddTagToTaskCommand(second, "FRONTEND"));

        Assert.Equal(a.Value.Id, b.Value.Id);
        Assert.Equal("Frontend", b.Value.Name); // le nom d'origine est conservé

        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.Tags.CountAsync());
    }

    [Fact]
    public async Task Adding_the_same_tag_twice_records_history_once()
    {
        var taskId = await CreateTaskAsync();

        await Handler().HandleAsync(new AddTagToTaskCommand(taskId, "Frontend"));
        var again = await Handler().HandleAsync(new AddTagToTaskCommand(taskId, "frontend"));

        Assert.True(again.IsSuccess);

        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.TaskActivities.CountAsync(a => a.TaskId == taskId && a.Type == TaskActivityType.TagAdded));
    }

    [Fact]
    public async Task Cannot_exceed_the_maximum_number_of_tags()
    {
        var taskId = await CreateTaskAsync();
        for (var i = 0; i < TaskItem.MaxTags; i++)
        {
            Assert.True((await Handler().HandleAsync(new AddTagToTaskCommand(taskId, $"Tag {i}"))).IsSuccess);
        }

        var result = await Handler().HandleAsync(new AddTagToTaskCommand(taskId, "One too many"));

        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abcdefghijklmnopqrstuvwxyz012345")] // 32 caractères
    public async Task Invalid_name_is_rejected(string name)
    {
        var taskId = await CreateTaskAsync();

        var result = await Handler().HandleAsync(new AddTagToTaskCommand(taskId, name));

        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Task_of_another_user_is_not_found()
    {
        var taskId = await CreateTaskAsync(owner: OwnerA);

        var result = await Handler(OwnerB).HandleAsync(new AddTagToTaskCommand(taskId, "Frontend"));

        Assert.Equal(ErrorType.NotFound, result.Error.Type);

        await using var db = fixture.CreateDbContext();
        Assert.Equal(0, await db.Tags.CountAsync()); // aucun tag créé au passage
    }

    private AddTagToTaskHandler Handler(string userId = OwnerA) => new(
        new TestDbContextFactory(fixture),
        new AddTagToTaskCommandValidator(),
        new FakeCurrentUser(userId),
        _time,
        new TaskChangeNotifier());

    private async Task<Guid> CreateTaskAsync(string owner = OwnerA)
    {
        var task = TaskItem.Create(owner, "Tâche", null, TaskPriority.Medium, null, _time.GetUtcNow().UtcDateTime, TaskType.Task);

        await using var db = fixture.CreateDbContext();
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task.Id;
    }
}
