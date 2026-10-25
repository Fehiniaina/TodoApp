using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Import;
using TodoApp.Features.Tasks.Shared;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Features.Tasks.Import;

[Collection("Postgres")]
public class ImportTasksTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Alice = "alice";
    private const string Bob = "bob";

    private readonly FakeTimeProvider _time =
        new(new DateTimeOffset(2026, 10, 24, 9, 0, 0, TimeSpan.Zero));

    private static readonly string[] expectedTitle = new[] { "Première", "Seconde" };

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(Alice, Bob);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Preview_resolves_known_assignee_and_flags_unknown_one()
    {
        var preview = await PreviewAsync(
            "Titre;Assigné\n" +
            $"Pour Bob;{EmailOf(Bob)}\n" +
            "Pour personne;inconnu@exemple.com\n");

        Assert.Equal(Bob, preview.Rows[0].AssigneeId);
        Assert.True(preview.Rows[0].IsValid);
        Assert.Contains(preview.Rows[1].Errors, e => e.Contains("inconnu@exemple.com", StringComparison.Ordinal));
        Assert.False(preview.CanImport);
    }

    [Fact]
    public async Task Import_creates_tasks_in_file_order_with_assignment_and_in_app_notification_only()
    {
        var preview = await PreviewAsync(
            "Titre;Priorité;Assigné\n" +
            $"Première;Haute;{EmailOf(Bob)}\n" +
            "Seconde;Basse;\n");

        var result = await ImportHandler(Alice).HandleAsync(new ImportTasksCommand(preview.Rows));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.Equal(2, result.Value);

        await using var db = fixture.CreateDbContext();

        var titles = await db.Tasks
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => t.Title)
            .ToListAsync();
        Assert.Equal(expectedTitle, titles);

        var first = await db.Tasks.SingleAsync(t => t.Title == "Première");
        Assert.Equal(Alice, first.CreatedById);
        Assert.Equal(Bob, first.AssigneeId);
        Assert.Equal(TaskPriority.High, first.Priority);

        var notification = await db.Notifications.Include(n => n.Email).SingleAsync();
        Assert.Equal(Bob, notification.RecipientId);
        Assert.Null(notification.Email); // import : cloche oui, email non
    }

    [Fact]
    public async Task Import_with_an_invalid_row_creates_nothing()
    {
        var preview = await PreviewAsync("Titre;Priorité\nValide;Haute\nInvalide;Hte\n");

        var result = await ImportHandler(Alice).HandleAsync(new ImportTasksCommand(preview.Rows));

        Assert.True(result.IsFailure);

        await using var db = fixture.CreateDbContext();
        Assert.False(await db.Tasks.AnyAsync());
    }

    private async Task<TaskImportPreview> PreviewAsync(string csv)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var handler = new PreviewTaskImportHandler(new TestDbContextFactory(fixture), new UpperInvariantLookupNormalizer());

        return await handler.HandleAsync(stream);
    }

    private ImportTasksHandler ImportHandler(string userId) => new(
        new TestDbContextFactory(fixture),
        new FakeCurrentUser(userId),
        _time,
        new TaskChangeNotifier(),
        NullLogger<ImportTasksHandler>.Instance);

    // Adapte au format d'email utilisé par PostgresFixture.CreateUsersAsync.
    private static string EmailOf(string userId) => $"{userId}@test.local";
}
