using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using TodoApp.Common;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Activity;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Activity;
using TodoApp.Features.Tasks.AssignTask;
using TodoApp.Features.Tasks.ChangeTaskStatus;
using TodoApp.Features.Tasks.CreateTask;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Features.Tasks.Activity;

[Collection("Postgres")]
public class TaskActivityHandlerTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string OwnerA = "owner-A";
    private const string OwnerB = "owner-B";
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 21, 9, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(OwnerA, OwnerB);
    }

    private CreateTaskHandler CreateHandler(string ownerId = OwnerA) => new(
        new TestDbContextFactory(fixture),
        new CreateTaskCommandValidator(),
        new FakeCurrentUser(ownerId),
        _time,
        new TaskChangeNotifier());

    private AssignTaskHandler AssignHandler(string ownerId = OwnerA) => new(
        new TestDbContextFactory(fixture),
        new FakeCurrentUser(ownerId),
        _time,
        new TaskChangeNotifier());

    private ChangeTaskStatusHandler StatusHandler(string ownerId = OwnerA) => new(
        new TestDbContextFactory(fixture),
        new FakeCurrentUser(ownerId),
        _time);

    private GetTaskActivityHandler ActivityHandler(string ownerId = OwnerA) => new(
        new TestDbContextFactory(fixture),
        new FakeCurrentUser(ownerId));

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task History_RecordsCreationAssignmentStatusesAndReassignment_InOrder()
    {
        var created = await CreateHandler(OwnerA).HandleAsync(TaskCommands.Create("TASK-1"));
        var taskId = created.Value.Id;

        await Step(AssignHandler(OwnerA).HandleAsync(new AssignTaskCommand(taskId, OwnerB)));
        await Step(StatusHandler(OwnerB).HandleAsync(new ChangeTaskStatusCommand(taskId, TaskItemStatus.InProgress)));
        await Step(StatusHandler(OwnerB).HandleAsync(new ChangeTaskStatusCommand(taskId, TaskItemStatus.InValidation)));
        await Step(AssignHandler(OwnerB).HandleAsync(new AssignTaskCommand(taskId, OwnerA)));

        var history = await ActivityHandler(OwnerA).HandleAsync(taskId);

        Assert.Equal(
            [TaskActivityType.Created, TaskActivityType.Assigned, TaskActivityType.StatusChanged,
         TaskActivityType.StatusChanged, TaskActivityType.Assigned],
            history.Value.Select(a => a.Type));

        var last = history.Value[^1];
        Assert.Equal((OwnerB, OwnerB, OwnerA), (last.ActorName, last.OldValue, last.NewValue));
    }

    [Fact]
    public async Task History_ForbiddenTransition_RecordsNothing()
    {
        var created = await CreateHandler(OwnerA).HandleAsync(TaskCommands.Create("Task"));

        await StatusHandler(OwnerA).HandleAsync(new ChangeTaskStatusCommand(created.Value.Id, TaskItemStatus.Resolved));

        var history = await ActivityHandler(OwnerA).HandleAsync(created.Value.Id);
        Assert.Equal(TaskActivityType.Created, Assert.Single(history.Value).Type);
    }

    [Fact]
    public async Task History_OfOtherUsersTask_ReturnsNotFound()
    {
        var created = await CreateHandler(OwnerA).HandleAsync(TaskCommands.Create("Private"));

        var result = await ActivityHandler(OwnerB).HandleAsync(created.Value.Id);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Theory]
    [InlineData(null, "Bob", "Alice a assigné la tâche à Bob")]
    [InlineData("Bob", "Alice", "Alice s'est assigné la tâche")]
    [InlineData("Bob", "Carl", "Alice a réassigné la tâche de Bob à Carl")]
    [InlineData("Bob", null, "Alice a retiré l'assignation")]
    public void Describe_Assignment(string? oldValue, string? newValue, string expected)
    {
        var dto = new TaskActivityDto(Guid.NewGuid(), TaskActivityType.Assigned, "Alice", oldValue, newValue, DateTime.UtcNow, null);

        Assert.Equal(expected, TaskActivityFormatter.Describe(dto));
    }

    /// <summary>Avance l'horloge (horodatages distincts) puis exécute une étape qui doit réussir.</summary>
    private async Task Step(Task<Result> action)
    {
        _time.Advance(TimeSpan.FromMinutes(1));
        var result = await action;
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
    }

    private async Task Step<T>(Task<Result<T>> action)
    {
        _time.Advance(TimeSpan.FromMinutes(1));
        var result = await action;
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
    }
}
