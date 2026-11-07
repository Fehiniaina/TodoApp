using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tags;
using TodoApp.Features.Tags.Shared;

namespace TodoApp.Features.Tags.SearchTags;

/// <summary>
/// Autocomplétion : les tags dont le nom contient le terme, ceux qui COMMENCENT par le terme en premier.
/// Sans terme : les premiers tags par ordre alphabétique.
/// </summary>
internal sealed class SearchTagsHandler(IDbContextFactory<ApplicationDbContext> factory) : IHandler
{
    private const int MaxLimit = 20;
    private const string LikeEscape = "\\";

    public async Task<IReadOnlyList<TagDto>> HandleAsync(SearchTagsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var limit = Math.Clamp(query.Limit, 1, MaxLimit);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var tags = db.Tags.AsQueryable();

        if (NormalizeTerm(query.Term) is { } term)
        {
            var escaped = EscapeLike(term);
            var contains = $"%{escaped}%";
            var startsWith = $"{escaped}%";

            tags = tags
                .Where(t => EF.Functions.Like(t.NormalizedName, contains, LikeEscape))
                .OrderByDescending(t => EF.Functions.Like(t.NormalizedName, startsWith, LikeEscape))
                .ThenBy(t => t.NormalizedName);
        }
        else
        {
            tags = tags.OrderBy(t => t.NormalizedName);
        }

        return await tags
            .Take(limit)
            .Select(t => new TagDto(t.Id, t.Name, t.Color))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Même normalisation que le nom stocké : la recherche ignore la casse et les espaces en trop.</summary>
    private static string? NormalizeTerm(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return null;
        }

        var trimmed = term.Length > Tag.MaxNameLength ? term[..Tag.MaxNameLength] : term;
        return Tag.IsValidName(trimmed) ? Tag.Normalize(trimmed) : null;
    }

    /// <summary>
    /// Neutralise les jokers de LIKE saisis par l'utilisateur : sans ça, chercher « % » ou « _ »
    /// renverrait TOUS les tags (et « 100% » trouverait « 1000 »).
    /// </summary>
    private static string EscapeLike(string value) =>
        value.Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
             .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
             .Replace("_", LikeEscape + "_", StringComparison.Ordinal);
}
