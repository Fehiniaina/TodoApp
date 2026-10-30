namespace TodoApp.Domain.Tags;

/// <summary>
/// Agrégat Tag : partagé par tous les utilisateurs. Les tâches le référencent par son Id,
/// sans navigation (un agrégat ne contient pas un autre agrégat).
/// Unicité du nom insensible à la casse et aux espaces superflus, via <see cref="NormalizedName"/>.
/// </summary>
internal sealed class Tag
{
    public const int MaxNameLength = 30;

    private Tag() { }   // EF Core

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public TagColor Color { get; private set; }
    public string CreatedById { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    public static Tag Create(string name, TagColor color, string createdById, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdById);

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be in UTC.", nameof(createdAtUtc));
        }

        var tag = new Tag
        {
            Id = Guid.CreateVersion7(),
            Color = EnsureDefined(color),
            CreatedById = createdById,
            CreatedAtUtc = createdAtUtc,
        };

        tag.SetName(name);
        return tag;
    }

    public void Rename(string name) => SetName(name);

    public void ChangeColor(TagColor color) => Color = EnsureDefined(color);

    /// <summary>Clé d'unicité : « front  end » et « FRONT END » désignent le même tag.</summary>
    public static string Normalize(string name) => Clean(name).ToUpperInvariant();

    private void SetName(string name)
    {
        var clean = Clean(name);
        Name = clean;
        NormalizedName = clean.ToUpperInvariant();
    }

    /// <summary>Retire les espaces en trop (début, fin, et répétés au milieu), puis vérifie la longueur.</summary>
    private static string Clean(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var clean = string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return clean.Length <= MaxNameLength
            ? clean
            : throw new ArgumentException($"A tag name cannot exceed {MaxNameLength} characters.", nameof(name));
    }

    private static TagColor EnsureDefined(TagColor color) =>
        Enum.IsDefined(color)
            ? color
            : throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown tag color.");
}
