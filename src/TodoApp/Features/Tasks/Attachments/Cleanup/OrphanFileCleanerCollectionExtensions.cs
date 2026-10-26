namespace TodoApp.Features.Tasks.Attachments.Cleanup;

internal static class OrphanFileCleanerCollectionExtensions
{
    public static IServiceCollection AddOrphanFileCleanerOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OrphanFileCleanupOptions>()
            .Bind(configuration.GetSection(OrphanFileCleanupOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<OrphanFileCleaner>();
        services.AddHostedService<OrphanFileCleanupService>();

        return services;
    }
}
