using System.Collections.Frozen;

namespace TodoApp.Features.Tasks.ListTasks;

/// <summary>
/// Correspondance stable valeur d'enum ⇄ slug d'URL. Les slugs sont PERSISTÉS chez les utilisateurs
/// (favoris, liens partagés) : un slug ne change jamais, même si le membre de l'enum est renommé.
/// Lecture insensible à la casse. Exhaustivité vérifiée à la construction.
/// </summary>
internal sealed class SlugMap<TEnum> where TEnum : struct, Enum
{
    private readonly FrozenDictionary<TEnum, string> _slugByValue;
    private readonly FrozenDictionary<string, TEnum> _valueBySlug;

    public SlugMap(IReadOnlyDictionary<TEnum, string> slugs, params TEnum[] excluded)
    {
        ArgumentNullException.ThrowIfNull(slugs);

        var missing = Enum.GetValues<TEnum>().Except(excluded).Where(v => !slugs.ContainsKey(v)).ToList();
        if (missing.Count > 0)
        {
            // Échoue au démarrage (initialisation statique) : un nouveau membre sans slug ne passe pas les tests.
            throw new InvalidOperationException(
                $"Slug manquant pour {typeof(TEnum).Name} : {string.Join(", ", missing)}.");
        }

        _slugByValue = slugs.ToFrozenDictionary();

        // ToFrozenDictionary lève une exception si deux valeurs partagent le même slug.
        _valueBySlug = slugs.ToFrozenDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<TEnum, string> All => _slugByValue;

    public string ToSlug(TEnum value) => _slugByValue[value];

    public bool TryParse(string? slug, out TEnum value) =>
        _valueBySlug.TryGetValue(slug ?? string.Empty, out value);
}
