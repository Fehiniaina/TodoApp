using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Respawn;
using Respawn.Graph;
using TodoApp.Data;
using Microsoft.Extensions.Configuration;

namespace TodoApp.IntegrationTests.Infrastructure;

/// <summary>
/// Uses a local PostgreSQL instance and a dedicated "<c>*_tests</c>" database.
/// Applies migrations once per test collection and exposes <see cref="ResetAsync"/>
/// so each test starts with a clean slate.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string TestDatabaseSuffix = "_tests";

    private Respawner _respawner = null!;

    public string ConnectionString { get; }

    public PostgresFixture()
    {
        // Reads the app's user secrets (same UserSecretsId as the TodoApp assembly),
        // then environment variables (e.g. ConnectionStrings__TestConnection in CI).
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ApplicationDbContext>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var baseConnectionString =
            configuration.GetConnectionString("TestConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No connection string found. Set ConnectionStrings:DefaultConnection in user secrets " +
                "or ConnectionStrings__TestConnection as an environment variable.");

        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString);
        var database = builder.Database ?? "todoapp";

        // Safety net: Respawn wipes every table — never run against the dev database.
        if (!database.EndsWith(TestDatabaseSuffix, StringComparison.OrdinalIgnoreCase))
        {
            database += TestDatabaseSuffix;
        }

        builder.Database = database;
        ConnectionString = builder.ConnectionString;
    }

    public async Task InitializeAsync()
    {
        await using (var db = CreateDbContext())
        {
            await db.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")],
        });

        await _respawner.ResetAsync(connection); // start from an empty DB even after an aborted run
    }

    public ApplicationDbContext CreateDbContext()
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseTodoAppNpgsql(ConnectionString)
            // EF Core 10 raises PendingModelChangesWarning as an exception during MigrateAsync
            // even when all migrations are up to date. Safe to suppress in tests: MigrateAsync
            // applies the correct schema regardless of the model-hash check.
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

        return new ApplicationDbContext(builder.Options);
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

/// <summary>xUnit collection that shares one <see cref="PostgresFixture"/> across all integration tests.</summary>
[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}
