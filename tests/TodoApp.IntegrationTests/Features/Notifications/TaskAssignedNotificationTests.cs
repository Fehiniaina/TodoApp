using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TodoApp.Data;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.AssignTask;
using TodoApp.Features.Tasks.CreateTask;
using TodoApp.Features.Tasks.DeleteTasks;
using TodoApp.Features.Tasks.Shared;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Features.Notifications;

[Collection("Postgres")]
public class TaskAssignedNotificationTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Alice = "alice";
    private const string Bob = "bob";
    private const string Carol = "carol";

    private readonly FakeTimeProvider _time =
        new(new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(Alice, Bob, Carol);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Tests ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Assigning_task_to_another_user_persists_unread_notification()
    {
        var taskId = await CreateTaskAsync(Alice);

        await AssignAsync(actor: Alice, taskId, assignee: Bob);

        await using var db = fixture.CreateDbContext();
        var notification = await db.Notifications.SingleAsync();

        Assert.Equal(Bob, notification.RecipientId);
        Assert.Equal(Alice, notification.ActorId);
        Assert.Equal(taskId, notification.TaskId);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, notification.CreatedAtUtc);
        Assert.Null(notification.ReadAtUtc);
    }

    [Fact]
    public async Task Self_assignment_creates_no_notification()
    {
        var taskId = await CreateTaskAsync(Alice);

        await AssignAsync(actor: Alice, taskId, assignee: Alice);

        await using var db = fixture.CreateDbContext();
        Assert.False(await db.Notifications.AnyAsync());
    }

    [Fact]
    public async Task Reassignment_notifies_only_the_new_assignee()
    {
        var taskId = await CreateTaskAsync(Alice);
        await AssignAsync(actor: Alice, taskId, assignee: Bob);

        await AssignAsync(actor: Alice, taskId, assignee: Carol);

        await using var db = fixture.CreateDbContext();
        var recipients = await db.Notifications
            .OrderBy(n => n.CreatedAtUtc)
            .Select(n => n.RecipientId)
            .ToListAsync();

        Assert.Equal(new[] { Bob, Carol }, recipients);
    }

    [Fact]
    public async Task Unassignment_creates_no_new_notification()
    {
        var taskId = await CreateTaskAsync(Alice);
        await AssignAsync(actor: Alice, taskId, assignee: Bob);

        await AssignAsync(actor: Alice, taskId, assignee: null);

        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.Notifications.CountAsync());
    }

    [Fact]
    public async Task Notifications_of_trashed_task_are_hidden()
    {
        var taskId = await CreateTaskAsync(Alice);
        await AssignAsync(actor: Alice, taskId, assignee: Bob);

        await TrashAsync(owner: Alice, taskId);

        await using var db = fixture.CreateDbContext();
        Assert.False(await db.Notifications.AnyAsync());
        Assert.Equal(1, await db.Notifications
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .CountAsync());
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Guid> CreateTaskAsync(string ownerId)
    {
        var handler = new CreateTaskHandler(
            new TestDbContextFactory(fixture),
            new CreateTaskCommandValidator(),
            new FakeCurrentUser(ownerId),
            _time,
            new TaskChangeNotifier());

        var result = await handler.HandleAsync(TaskCommands.Create("Tâche notifiée"));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value.Id;
    }

    private async Task AssignAsync(string actor, Guid taskId, string? assignee)
    {
        _time.Advance(TimeSpan.FromMinutes(1)); // horodatages distincts → ordre fiable

        var handler = new AssignTaskHandler(
            new TestDbContextFactory(fixture),
            new FakeCurrentUser(actor),
            _time,
            new TaskChangeNotifier());

        var result = await handler.HandleAsync(new AssignTaskCommand(taskId, assignee));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
    }

    private async Task TrashAsync(string owner, Guid taskId)
    {
        var handler = new DeleteTasksHandler(
            new TestDbContextFactory(fixture),
            new TaskChangeNotifier(),
            _time,
            new FakeCurrentUser(owner));

        var result = await handler.HandleAsync(new DeleteTasksCommand(taskId));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
    }
}
