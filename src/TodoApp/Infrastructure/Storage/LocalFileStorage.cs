using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace TodoApp.Infrastructure.Storage;

internal sealed partial class LocalFileStorage(
    IOptions<FileStorageOptions> options,
    TimeProvider timeProvider) : IFileStorage
{
    [GeneratedRegex(@"^\d{4}/\d{2}/[0-9a-f]{32}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex KeyPattern();

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var now = timeProvider.GetUtcNow();
        var key = string.Create(CultureInfo.InvariantCulture, $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}");
        var path = ResolvePath(key);
        var temporaryPath = path + ".uploading";

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        try
        {
            await using (var file = new FileStream(temporaryPath, new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous,
            }))
            {
                await content.CopyToAsync(file, cancellationToken);
            }

            File.Move(temporaryPath, path);
            return key;
        }
        catch
        {
            File.Delete(temporaryPath);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);

        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
        });

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        File.Delete(ResolvePath(key)); // ne lève pas d'exception si le fichier n'existe pas
        return Task.CompletedTask;
    }

    private string ResolvePath(string key)
    {
        if (!KeyPattern().IsMatch(key))
        {
            throw new ArgumentException("Clé de stockage invalide.", nameof(key));
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.Value.RootPath));
        var path = Path.GetFullPath(Path.Combine(root, key));

        // Défense en profondeur : même si la regex changeait, le chemin final reste sous la racine.
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("Clé de stockage hors du dossier autorisé.", nameof(key));
        }

        return path;
    }
}
