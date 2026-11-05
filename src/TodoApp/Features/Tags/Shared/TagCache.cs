namespace TodoApp.Features.Tags.Shared;

/// <summary>
/// Cache de la liste des tags. Clé partagée entre le lecteur (GetAllTagsHandler)
/// et l'invalidation (TagCacheInvalidationInterceptor) : impossible qu'ils divergent.
/// </summary>
internal static class TagCache
{
    public const string AllTagsKey = "tags:all";

    /// <summary>
    /// Filet de sécurité, pas mécanisme principal : l'invalidation suit les modifications faites via EF.
    /// L'expiration rattrape celles qui contournent le suivi des entités (ExecuteUpdate/Delete, SQL brut).
    /// </summary>
    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);
}
