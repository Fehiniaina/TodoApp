using FluentValidation;
using TodoApp.Common;
using TodoApp.Features.Shared;

namespace TodoApp.Features;

internal static class DependencyInjection
{
    /// <summary>
    /// Single composition entry point for the task feature set: real-time
    /// notifier, auto-scanned handlers and validators. Handlers and validators
    /// are discovered by convention — never registered by hand.
    /// </summary>
    public static IServiceCollection AddTaskFeatures(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<TaskChangeNotifier>();
        services.RegisterHandlersFromAssemblyContaining<Program>();
        services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);

        return services;
    }
}
