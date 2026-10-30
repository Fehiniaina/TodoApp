namespace TodoApp.Domain.Tags;

/// <summary>
/// Couleur attribuée à un tag créé à la volée : stable pour un même nom, répartie sur la palette.
/// </summary>
internal static class TagPalette
{
    private static readonly TagColor[] Colors =
    [
        TagColor.Blue, TagColor.Green, TagColor.Yellow, TagColor.Orange, TagColor.Red, TagColor.Purple,
    ];

    public static TagColor For(string normalizedName)
    {
        ArgumentNullException.ThrowIfNull(normalizedName);

        // Pas de string.GetHashCode() : il est ALÉATOIRE par processus en .NET (protection contre
        // les attaques par collision). La même étiquette changerait de couleur à chaque redémarrage.
        var hash = 0;
        foreach (var c in normalizedName)
        {
            hash = unchecked((hash * 31) + c);
        }

        return Colors[(uint)hash % (uint)Colors.Length];
    }
}
