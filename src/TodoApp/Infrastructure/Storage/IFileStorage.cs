namespace TodoApp.Infrastructure.Storage;

/// <summary>Stockage binaire. Le domaine ne connaît que la clé, jamais le chemin.</summary>
internal interface IFileStorage
{
    /// <summary>Écrit le contenu et renvoie une clé opaque générée par le stockage.</summary>
    Task<string> SaveAsync(Stream content, CancellationToken cancellationToken);

    /// <summary>Ouvre le contenu en lecture, ou <c>null</c> s'il n'existe plus.</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken);

    /// <summary>Supprime le contenu. Sans effet s'il n'existe pas.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
