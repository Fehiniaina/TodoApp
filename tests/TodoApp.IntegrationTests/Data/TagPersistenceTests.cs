using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Tags;
using TodoApp.Domain.Tasks;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Data;

[Collection("Postgres")]
public sealed class TagPersistenceTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Owner = "owner-A";
    private static readonly DateTime Now = new(2026, 10, 30, 9, 0, 0, DateTimeKind.Utc);

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(Owner);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Tag_names_are_unique_ignoring_case_and_spaces()
    {
        await SaveAsync(Tag.Create("Frontend", TagColor.Blue, Owner, Now));

        // La garantie vient de l'index unique : c'est elle qui protège contre deux créations simultanées.
        await Assert.ThrowsAsync<DbUpdateException>(
            () => SaveAsync(Tag.Create("  FRONTEND ", TagColor.Red, Owner, Now)));
    }

    [Fact]
    public async Task Task_tags_are_persisted_and_reloaded()
    {
        var frontend = Tag.Create("Frontend", TagColor.Blue, Owner, Now);
        var urgent = Tag.Create("Client", TagColor.Red, Owner, Now);
        await SaveAsync(frontend, urgent);

        var task = TaskItem.Create(Owner, "Tâche", null, TaskPriority.Medium, null, Now, TaskType.Task);
        task.AddTag(frontend, Owner, Now);
        task.AddTag(urgent, Owner, Now);
        await SaveAsync(task);

        await using var db = fixture.CreateDbContext();
        var reloaded = await db.Tasks.SingleAsync(t => t.Id == task.Id);

        Assert.Equal(
            new[] { frontend.Id, urgent.Id }.Order(),
            reloaded.Tags.Select(t => t.TagId).Order());
    }

    [Fact]
    public async Task Deleting_a_tag_removes_it_from_tasks()
    {
        var tag = Tag.Create("Frontend", TagColor.Blue, Owner, Now);
        await SaveAsync(tag);

        var task = TaskItem.Create(Owner, "Tâche", null, TaskPriority.Medium, null, Now, TaskType.Task);
        task.AddTag(tag, Owner, Now);
        await SaveAsync(task);

        await using (var db = fixture.CreateDbContext())
        {
            await db.Tags.Where(t => t.Id == tag.Id).ExecuteDeleteAsync();
        }

        await using (var db = fixture.CreateDbContext())
        {
            var reloaded = await db.Tasks.SingleAsync(t => t.Id == task.Id);
            Assert.Empty(reloaded.Tags);
        }
    }

    [Fact]
    public async Task Adding_a_tag_is_recorded_in_history_with_its_name()
    {
        var tag = Tag.Create("Frontend", TagColor.Blue, Owner, Now);
        await SaveAsync(tag);

        var task = TaskItem.Create(Owner, "Tâche", null, TaskPriority.Medium, null, Now, TaskType.Task);
        task.AddTag(tag, Owner, Now);
        await SaveAsync(task);

        await using var db = fixture.CreateDbContext();
        var activity = await db.TaskActivities
            .SingleAsync(a => a.TaskId == task.Id && a.Type == Domain.Tasks.Activity.TaskActivityType.TagAdded);

        Assert.Equal("Frontend", activity.NewValue);
    }

    private async Task SaveAsync(params object[] entities)
    {
        await using var db = fixture.CreateDbContext();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }
}
