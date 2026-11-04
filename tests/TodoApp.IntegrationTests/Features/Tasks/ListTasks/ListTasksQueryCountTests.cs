using TodoApp.Domain.Tags;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.ListTasks;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Features.Tasks.ListTasks;

[Collection("Postgres")]
public sealed class ListTasksQueryCountTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Owner = "owner-A";
    private static readonly DateOnly Today = new(2026, 11, 4);
    private static readonly DateTime Now = new(2026, 11, 4, 9, 0, 0, DateTimeKind.Utc);

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(Owner);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(3)]
    [InlineData(30)]
    public async Task Number_of_queries_does_not_depend_on_number_of_tasks(int taskCount)
    {
        await SeedTasksWithTagsAsync(taskCount);

        var counter = new CommandCounter();
        var handler = new ListTasksHandler(
            new InterceptedDbContextFactory(fixture, counter),
            new FakeCurrentUser(Owner));

        var result = await handler.HandleAsync(
            new ListTasksQuery(TaskScope.All, Page: 1, PageSize: 100) { Filter = TaskFilter.None, Today = Today });

        Assert.True(result.IsSuccess);
        Assert.Equal(taskCount, result.Value.Items.Count);
        Assert.All(result.Value.Items, t => Assert.Equal(["Backend", "Frontend"], t.Tags.Select(tag => tag.Name)));

        // 1 requête pour le total + 1 pour la page (tâches ET leurs tags), quel que soit le nombre de tâches.
        Assert.Equal(2, counter.Count);
    }

    private async Task SeedTasksWithTagsAsync(int taskCount)
    {
        var frontend = Tag.Create("Frontend", TagColor.Blue, Owner, Now);
        var backend = Tag.Create("Backend", TagColor.Green, Owner, Now);

        await using var db = fixture.CreateDbContext();
        db.Tags.AddRange(frontend, backend);

        for (var i = 0; i < taskCount; i++)
        {
            var task = TaskItem.Create(Owner, $"Tâche {i}", null, TaskPriority.Medium, null, Now.AddMinutes(i), TaskType.Task);
            task.AddTag(frontend, Owner, Now);
            task.AddTag(backend, Owner, Now);
            db.Tasks.Add(task);
        }

        await db.SaveChangesAsync();
    }
}
