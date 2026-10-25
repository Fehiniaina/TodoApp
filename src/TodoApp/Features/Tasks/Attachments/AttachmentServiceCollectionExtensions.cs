using Microsoft.AspNetCore.Identity;
using TodoApp.Infrastructure.Storage;

namespace TodoApp.Features.Tasks.Attachments;

internal static class AttachmentServiceCollectionExtensions
{
    public static IServiceCollection AddAttachmentFeatures(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .PostConfigure<IWebHostEnvironment>((options, env) =>
            {
                if (!Path.IsPathRooted(options.RootPath))
                {
                    options.RootPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.RootPath));
                }
            })
            .Validate<IWebHostEnvironment>(
                (options, env) => !IsUnder(options.RootPath, env.WebRootPath),
                "FileStorage:RootPath ne doit pas être dans wwwroot : les fichiers y seraient publics.")
            .ValidateOnStart();

        services.AddSingleton<IFileStorage, LocalFileStorage>();

        return services;
    }

    private static bool IsUnder(string path, string? parent) =>
        !string.IsNullOrEmpty(parent)
        && Path.GetFullPath(path).StartsWith(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent)) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
}
