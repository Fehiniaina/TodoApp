using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;
using TodoApp.Data;
using TodoApp.Infrastructure.Identity;

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

        await _respawner.ResetAsync(connection);
    }

    public ApplicationDbContext CreateDbContext(params IInterceptor[] interceptors)
    {
        ArgumentNullException.ThrowIfNull(interceptors);

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseTodoAppNpgsql(ConnectionString)
            // Identity lit ses options de schéma dans ce fournisseur : même modèle que l'application.
            .UseApplicationServiceProvider(IdentityServices);

        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return new ApplicationDbContext(builder.Options);
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public async Task CreateUsersAsync(params string[] userIds)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        await using var db = CreateDbContext();

        foreach (var id in userIds)
        {
            db.Users.Add(new ApplicationUser
            {
                Id = id,
                UserName = $"{id}@test.local",
                NormalizedUserName = $"{id}@TEST.LOCAL".ToUpperInvariant(),
                Email = $"{id}@test.local",
                NormalizedEmail = $"{id}@TEST.LOCAL".ToUpperInvariant(),
                FirstName = id,
            });
        }

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly IServiceProvider IdentityServices = new ServiceCollection()
        .Configure<IdentityOptions>(options => IdentityStoreConfiguration.Configure(options.Stores))
        .BuildServiceProvider();
}
