using TodoApp.Common;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.AssignTask;
using TodoApp.Features.Tasks.CreateTask;
using TodoApp.Features.Tasks.DeleteTasks;
using TodoApp.Features.Tasks.ListTasks;
using TodoApp.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Time.Testing;

namespace TodoApp.IntegrationTests.Features.Tasks.AssignTask;

[Collection("Postgres")]
public class AssignTaskHandlerTests(PostgresFixture fixture) : IAsyncLifetime
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

    private ListTasksHandler ListHandler(string ownerId = OwnerA) => new(
            new TestDbContextFactory(fixture),
            new FakeCurrentUser(ownerId));

    private DeleteTasksHandler DeleteHandler(string ownerId = OwnerA) => new(
        new TestDbContextFactory(fixture),
        new TaskChangeNotifier(),
        _time,
        new FakeCurrentUser(ownerId));

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Assign_ToOtherUser_TaskBecomesVisibleToAssignee()
    {
        var created = await CreateHandler(OwnerA).HandleAsync(TaskCommands.Create("Shared task"));

        var result = await AssignHandler(OwnerA).HandleAsync(new AssignTaskCommand(created.Value.Id, OwnerB));
        Assert.True(result.IsSuccess);

        var listAsB = await ListHandler(OwnerB).HandleAsync(new ListTasksQuery(TaskScope.AssignedToMe, 1, 10));
        Assert.Equal(created.Value.Id, Assert.Single(listAsB.Value.Items).Id);
    }

    [Fact]
    public async Task Unassigned_TaskNoLongerVisibleToFormerAssignee()
    {
        var created = await CreateHandler(OwnerA).HandleAsync(TaskCommands.Create("Shared task"));
        await AssignHandler(OwnerA).HandleAsync(new AssignTaskCommand(created.Value.Id, OwnerB));

        await AssignHandler(OwnerA).HandleAsync(new AssignTaskCommand(created.Value.Id, AssigneeId: null));

        var listAsB = await ListHandler(OwnerB).HandleAsync(new ListTasksQuery(TaskScope.All, 1, 10));
        Assert.Empty(listAsB.Value.Items);
    }

    [Fact]
    public async Task Assignee_CanReassignToCreator()
    {
        var created = await CreateHandler(OwnerA).HandleAsync(TaskCommands.Create("Ping-pong"));
        await AssignHandler(OwnerA).HandleAsync(new AssignTaskCommand(created.Value.Id, OwnerB));

        var result = await AssignHandler(OwnerB).HandleAsync(new AssignTaskCommand(created.Value.Id, OwnerA));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Assignee_CannotMoveTaskToTrash()
    {
        var created = await CreateHandler(OwnerA).HandleAsync(TaskCommands.Create("Not yours to delete"));
        await AssignHandler(OwnerA).HandleAsync(new AssignTaskCommand(created.Value.Id, OwnerB));

        var result = await DeleteHandler(OwnerB).HandleAsync(new DeleteTasksCommand(created.Value.Id));

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Assign_ToUnknownUser_ReturnsValidationError()
    {
        var created = await CreateHandler(OwnerA).HandleAsync(TaskCommands.Create("Task"));

        var result = await AssignHandler(OwnerA).HandleAsync(new AssignTaskCommand(created.Value.Id, "ghost"));

        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
