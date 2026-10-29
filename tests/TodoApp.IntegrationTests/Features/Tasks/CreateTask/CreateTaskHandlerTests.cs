using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.CreateTask;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Features.Tasks.CreateTask;

[Collection("Postgres")]
public class CreateTaskHandlerTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string OwnerA = "owner-A";
    private const string OwnerB = "owner-B";
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 21, 9, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(OwnerA, OwnerB);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private CreateTaskHandler CreateHandler(string ownerId = OwnerA) => new(
        new TestDbContextFactory(fixture),
        new CreateTaskCommandValidator(),
        new FakeCurrentUser(ownerId),
        _time,
        new TaskChangeNotifier());

    [Fact]
    public void Diagnostic_PriorityIsMappedToPostgresEnum()
    {
        using var fromFixture = fixture.CreateDbContext();
        using var fromFactory = new TestDbContextFactory(fixture).CreateDbContext();

        string? ColumnType(ApplicationDbContext db) =>
            db.Model.FindEntityType(typeof(TaskItem))!
              .FindProperty(nameof(TaskItem.Priority))!
              .GetColumnType();

        Assert.Equal("task_priority", ColumnType(fromFixture));
        Assert.Equal("task_priority", ColumnType(fromFactory));
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSuccess()
    {
        var command = new CreateTaskCommand("Buy milk", null, TaskPriority.Low);

        var result = await CreateHandler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(command.Title, result.Value.Title);
        Assert.Equal(command.Priority, result.Value.Priority);
        Assert.Null(result.Value.Description);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_PersistsTaskToDatabase()
    {
        var command = new CreateTaskCommand("Write tests", "Important!", TaskPriority.High);

        var result = await CreateHandler().HandleAsync(command);

        await using var db = fixture.CreateDbContext();
        var persisted = await db.Tasks.AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == result.Value.Id);

        Assert.NotNull(persisted);
        Assert.Equal("Write tests", persisted.Title);
        Assert.Equal("Important!", persisted.Description);
        Assert.Equal(TaskPriority.High, persisted.Priority);
        Assert.Equal(OwnerA, persisted.CreatedById);
    }

    [Fact]
    public async Task HandleAsync_EmptyTitle_ReturnsValidationFailure()
    {
        var command = new CreateTaskCommand("", null, TaskPriority.Medium);

        var result = await CreateHandler().HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_InvalidPriority_ReturnsValidationFailure()
    {
        var command = new CreateTaskCommand("Valid title", null, (TaskPriority)999);

        var result = await CreateHandler().HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_TasksAreScopedByOwner_OtherOwnerSeesNoTasks()
    {
        // Arrange: create a task for ownerA
        var commandA = new CreateTaskCommand("Secret task", null, TaskPriority.Medium);
        await CreateHandler().HandleAsync(commandA);

        // Act: query database as ownerB
        await using var db = fixture.CreateDbContext();
        var ownerBTasks = await db.Tasks.AsNoTracking()
            .Where(t => t.CreatedById == "owner-B")
            .ToListAsync();

        // Assert: ownerB has no tasks
        Assert.Empty(ownerBTasks);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_PersistsTaskWithCurrentUserAsOwner()
    {
        var command = new CreateTaskCommand("Write tests", "Important!", TaskPriority.High);

        var result = await CreateHandler().HandleAsync(command);

        Assert.True(result.IsSuccess);

        await using var db = fixture.CreateDbContext();
        var persisted = await db.Tasks.AsNoTracking()
           .SingleOrDefaultAsync(t => t.Id == result.Value.Id);

        Assert.NotNull(persisted);
        Assert.Equal("Write tests", persisted.Title);
        Assert.Equal("Important!", persisted.Description);
        Assert.Equal(TaskPriority.High, persisted.Priority);
        Assert.Equal(OwnerA, persisted.CreatedById);
    }

    [Fact]
    public async Task HandleAsync_TaskCreatedByOwnerA_IsNotVisibleToOwnerB()
    {
        // Arrange
        var result = await CreateHandler(OwnerB)
            .HandleAsync(new CreateTaskCommand("Write tests", "Important!", TaskPriority.High));

        Assert.True(result.IsSuccess);

        // Act
        await using var db = fixture.CreateDbContext();
        var ownerATasks = await db.Tasks.AsNoTracking().Where(t => t.CreatedById == OwnerA).ToListAsync();
        var ownerBTasks = await db.Tasks.AsNoTracking().Where(t => t.CreatedById == OwnerB).ToListAsync();

        // Assert
        var task = Assert.Single(ownerBTasks);
        Assert.Equal(result.Value.Id, task.Id);
        Assert.Empty(ownerATasks);
    }
}
