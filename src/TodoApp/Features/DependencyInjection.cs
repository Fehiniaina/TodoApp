using FluentValidation;
using TodoApp.Common;
using TodoApp.Features.Shared;
using TodoApp.Infrastructure.Seed;

namespace TodoApp.Features;

internal static class DependencyInjection
{
    /// <summary>
    /// Single composition entry point for the task feature set: real-time
    /// notifier, auto-scanned handlers and validators, and database seeding.
    /// Handlers and validators are discovered by convention — never registered by hand.
    /// </summary>
    public static IServiceCollection AddTaskFeatures(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<TaskChangeNotifier>();
        services.RegisterHandlersFromAssemblyContaining<Program>();
        services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);

        // Register database seeder as hosted service (runs once at startup in Development only)

#pragma warning disable S125 // Sections of code should not be commented out
                            //services.AddHostedService<DatabaseSeederHostedService>();

        return services;
#pragma warning restore S125 // Sections of code should not be commented out
    }
}
