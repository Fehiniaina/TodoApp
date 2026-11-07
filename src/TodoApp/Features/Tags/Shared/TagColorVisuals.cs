using System.Collections.Frozen;
using TodoApp.Domain.Tags;

namespace TodoApp.Features.Tags.Shared;

/// <summary>
/// Classe CSS par couleur, explicite.
/// au nom du membre. Exhaustivité vérifiée par TagColorVisualsTests.
/// </summary>
internal static class TagColorVisuals
{
    private const string Fallback = "tag-color--gray";

    private static readonly FrozenDictionary<TagColor, string> CssByColor =
        new Dictionary<TagColor, string>
        {
            [TagColor.Gray] = "tag-color--gray",
            [TagColor.Blue] = "tag-color--blue",
            [TagColor.Green] = "tag-color--green",
            [TagColor.Yellow] = "tag-color--yellow",
            [TagColor.Orange] = "tag-color--orange",
            [TagColor.Red] = "tag-color--red",
            [TagColor.Purple] = "tag-color--purple",
        }.ToFrozenDictionary();

    public static string CssFor(TagColor color) =>
        CssByColor.TryGetValue(color, out var css) ? css : Fallback;

    internal static bool HasVisual(TagColor color) => CssByColor.ContainsKey(color);
}
