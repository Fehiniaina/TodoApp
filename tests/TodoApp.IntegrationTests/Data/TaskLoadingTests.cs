using Microsoft.EntityFrameworkCore;
using Npgsql;
using TodoApp.Domain.Tags;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.Shared;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Data;

[Collection("Postgres")]
public sealed class TaskLoadingTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Owner = "owner-A";
    private const int ChecklistItems = 50;
    private const int TagCount = 10;
    private static readonly DateTime Now = new(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc);

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(Owner);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Loading_for_update_does_not_multiply_rows()
    {
        var taskId = await SeedTaskAsync();

        var capture = new CommandCapture();
        TaskItem task;
        await using (var db = fixture.CreateDbContext(capture))
        {
            task = await db.Tasks.ForUpdate().SingleAsync(t => t.Id == taskId);
        }

        // L'agrégat est complet…
        Assert.Equal(ChecklistItems, task.Checklist.Count);
        Assert.Equal(TagCount, task.Tags.Count);

        // … et la base renvoie une ligne par élément, pas leur produit (500 avec une requête unique).
        long rows = 0;
        foreach (var (sql, parameters) in capture.Commands)
        {
            rows += await CountRowsAsync(sql, parameters);
        }

        Assert.Equal(1 + ChecklistItems + TagCount, rows);
    }

    /// <summary>Exécute le SQL produit par EF, enveloppé dans un COUNT(*), avec les mêmes paramètres.</summary>
    private async Task<long> CountRowsAsync(string sql, (string Name, object? Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

#pragma warning disable CA2100, S2077 // SQL généré par EF, pas de donnée utilisateur.
        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM ({sql}) AS q", connection);
#pragma warning restore CA2100, S2077

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<Guid> SeedTaskAsync()
    {
        await using var db = fixture.CreateDbContext();

        var task = TaskItem.Create(Owner, "Tâche chargée", new string('x', 2000), TaskPriority.Medium, null, Now, TaskType.Task);

        for (var i = 0; i < ChecklistItems; i++)
        {
            task.AddChecklistItem($"Étape {i}");
        }

        for (var i = 0; i < TagCount; i++)
        {
            var tag = Tag.Create($"Tag {i}", TagColor.Blue, Owner, Now);
            db.Tags.Add(tag);
            task.AddTag(tag, Owner, Now);
        }

        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task.Id;
    }
}
