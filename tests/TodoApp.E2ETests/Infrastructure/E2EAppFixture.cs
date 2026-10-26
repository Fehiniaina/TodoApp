using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Npgsql;
using Respawn;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.Attachments.Cleanup;

namespace TodoApp.E2ETests.Infrastructure;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "xUnit libère la fixture via IAsyncLifetime.DisposeAsync, qui dispose _factory.")]
public sealed class E2EAppFixture : IAsyncLifetime
{
    public const string Password = "E2e-Passw0rd!";

    private readonly string _storageRoot =
        Path.Combine(Path.GetTempPath(), "todoapp-e2e", Guid.NewGuid().ToString("N"));

    private E2EWebApplicationFactory _factory = null!;
    private Respawner _respawner = null!;
    private string _connectionString = null!;

    public Uri BaseUrl { get; private set; } = null!;

    internal FakeEmailTransport Emails { get; } = new();

    public async Task InitializeAsync()
    {
        _connectionString = new ConfigurationBuilder()
            .AddUserSecrets<E2EAppFixture>()
            .AddEnvironmentVariables()
            .Build()
            .GetConnectionString("E2E")
            ?? throw new InvalidOperationException("ConnectionStrings:E2E manquant (user-secrets du projet E2E).");

        // Les variables d'environnement passent AVANT appsettings et les user-secrets de l'app :
        // c'est le moyen le plus sûr d'être lu par Program.cs dès la construction du builder.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _connectionString);
        Environment.SetEnvironmentVariable("FileStorage__RootPath", _storageRoot);

        _factory = new E2EWebApplicationFactory(Emails);
        _factory.UseKestrel();
        _factory.StartServer();

        var serverAddress = new Uri(_factory.Services
            .GetRequiredService<IServer>()
            .Features.GetRequiredFeature<IServerAddressesFeature>()
            .Addresses.First());

        BaseUrl = new UriBuilder(serverAddress) { Host = "localhost" }.Uri;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"],
        });

        // Blazor Server : premier rendu parfois lent sur une machine chargée.
        Assertions.SetDefaultExpectTimeout(10_000);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        Environment.SetEnvironmentVariable("FileStorage__RootPath", null);

        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }

    /// <summary>Base vide, stockage vide, aucun email : chaque test part de zéro.</summary>
    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);

        Emails.Clear();

        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }

    public async Task<TestUser> CreateUserAsync(string firstName, string lastName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"{firstName}.{lastName}@e2e.test";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
        };

        var result = await users.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return new TestUser(user.Id, email, Password, $"{firstName} {lastName}");
    }

    public async Task<Guid> CreateTaskAsync(TestUser owner, string title, string? description = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        ArgumentNullException.ThrowIfNull(owner);
        var task = TaskItem.Create(owner.Id, title, description, TaskPriority.Medium, null, DateTime.UtcNow);
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        return task.Id;
    }

    /// <summary>Simule le passage du temps sur les fichiers stockés (le délai de grâce compte sur leur date).</summary>
    public void AgeStoredFiles(TimeSpan age)
    {
        if (!Directory.Exists(_storageRoot))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(_storageRoot, "*", SearchOption.AllDirectories))
        {
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow - age);
        }
    }

    public int StoredFileCount() =>
        Directory.Exists(_storageRoot)
            ? Directory.GetFiles(_storageRoot, "*", SearchOption.AllDirectories).Length
            : 0;

    public async Task PurgeTaskAsync(Guid taskId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        await db.Tasks.IgnoreQueryFilters().Where(t => t.Id == taskId).ExecuteDeleteAsync();
    }

    public async Task<int> RunOrphanFileCleanupAsync()
    {
        var cleaner = _factory.Services.GetRequiredService<OrphanFileCleaner>();
        return await cleaner.RunOnceAsync(CancellationToken.None);
    }
}
