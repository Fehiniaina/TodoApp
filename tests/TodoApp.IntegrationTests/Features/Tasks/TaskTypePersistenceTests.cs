using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.CreateTask;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Features.Tasks;

[Collection("Postgres")]
public class TaskTypePersistenceTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string OwnerA = "owner-A";
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 21, 9, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(OwnerA);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Bug_type_is_persisted_as_bug()
    {
        // Régression du piège « sentinelle » : sans HasSentinel, Bug (= 0) n'est pas envoyé
        // à l'INSERT et la base applique son défaut (Task).
        var task = TaskItem.Create(
            OwnerA,
            "test-title",
            "test-description",
            TaskPriority.Low,
            dueDate: null,
            createdAtUtc: _time.GetUtcNow().UtcDateTime,
            type: TaskType.Bug);

        await using (var db = fixture.CreateDbContext())
        {
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateDbContext())
        {
            var reloaded = await db.Tasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
            Assert.Equal(TaskType.Bug, reloaded.Type);
        }
    }

    [Fact]
    public async Task Type_column_defaults_to_task_in_database()
    {
        // Régression du piège « defaultValue: 0 » : une ligne insérée sans type
        // (script, import SQL, autre application) doit recevoir Task, pas Bug.
        await using var db = fixture.CreateDbContext();

        var columnDefault = await db.Database
            .SqlQuery<string>($"""
                SELECT column_default AS "Value"
                FROM information_schema.columns
                WHERE table_name = 'tasks' AND column_name = 'type'
                """)
            .SingleAsync();

        Assert.Equal("3", columnDefault);
    }

}
