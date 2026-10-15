using Microsoft.Extensions.DependencyInjection;

namespace TodoApp.Common;

internal static class HandlerRegistration
{
    /// <summary>
    /// Registers every concrete <see cref="IHandler"/> found in the assembly
    /// containing <typeparamref name="T"/> as a scoped service against its own
    /// type, so components can inject the concrete handler directly.
    /// </summary>
    public static IServiceCollection RegisterHandlersFromAssemblyContaining<T>(this IServiceCollection services)
    {
        var handlerTypes = typeof(T).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IHandler).IsAssignableFrom(type));

        foreach (var handlerType in handlerTypes)
        {
            services.AddScoped(handlerType);
        }

        return services;
    }
}
