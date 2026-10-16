using Microsoft.EntityFrameworkCore;
using TodoApp.Data;

namespace TodoApp.Infrastructure.Seed;

/// <summary>
/// Hosted service that runs database seeding once at application startup,
/// only in Development environment. Keeps seeding logic decoupled from Program.cs.
/// </summary>
internal sealed class DatabaseSeederHostedService(
    IDbContextFactory<ApplicationDbContext> factory,
    IHostEnvironment environment,
    ILogger<DatabaseSeederHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        try
        {
            logger.LogInformation("Database seeding started...");
            await TodoAppSeeder.SeedAsync(factory);
            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database seeding.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
