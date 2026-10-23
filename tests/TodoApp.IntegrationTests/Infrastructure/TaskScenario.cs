using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.AssignTask;
using TodoApp.Features.Tasks.CreateTask;
using TodoApp.Features.Tasks.DeleteTasks;

namespace TodoApp.IntegrationTests.Infrastructure;

internal sealed class TaskScenario(PostgresFixture fixture, FakeTimeProvider time)
{
    public async Task<Guid> CreateTaskAsync(string ownerId, string title = "Tâche notifiée")
    {
        var handler = new CreateTaskHandler(
            new TestDbContextFactory(fixture),
            new CreateTaskCommandValidator(),
            new FakeCurrentUser(ownerId),
            time,
            new TaskChangeNotifier());

        var result = await handler.HandleAsync(TaskCommands.Create(title));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value.Id;
    }

    public async Task AssignAsync(string actor, Guid taskId, string? assignee)
    {
        time.Advance(TimeSpan.FromMinutes(1));

        var handler = new AssignTaskHandler(
            new TestDbContextFactory(fixture),
            new FakeCurrentUser(actor),
            time,
            new TaskChangeNotifier());

        var result = await handler.HandleAsync(new AssignTaskCommand(taskId, assignee));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
    }

    public async Task TrashAsync(string owner, Guid taskId)
    {
        var handler = new DeleteTasksHandler(
            new TestDbContextFactory(fixture),
            new TaskChangeNotifier(),
            time,
            new FakeCurrentUser(owner));

        var result = await handler.HandleAsync(new DeleteTasksCommand(taskId));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
    }
}
