using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;

namespace TodoApp.Infrastructure.Seed;

/// <summary>
/// Hosted service that runs database seeding once at application startup,
/// only in Development environment. Keeps seeding logic decoupled from Program.cs.
/// </summary>
internal sealed partial class DatabaseSeederHostedService(
    IDbContextFactory<ApplicationDbContext> factory,
    IHostEnvironment environment,
    ILogger<DatabaseSeederHostedService> logger) : IHostedService
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Un seed raté en dev ne doit pas empêcher le démarrage ; l'erreur est journalisée.")]
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        try
        {
            await TodoAppSeeder.SeedAsync(factory, cancellationToken);
        }
        catch (Exception ex)
        {
            LogOnException(ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred during database seeding.")]
    private partial void LogOnException(Exception ex);
}
