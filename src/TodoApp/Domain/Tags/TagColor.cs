namespace TodoApp.Domain.Tags;

/// <summary>
/// Couleur d'un tag. Valeurs PERSISTÉES : ne jamais modifier un nombre existant
/// (contrat vérifié par TagColorContractTests). 0 = Gray : une valeur NEUTRE en zéro,
/// pour qu'une valeur « non renseignée » ne devienne pas une couleur plausible (leçon du piège 1).
/// </summary>
public enum TagColor
{
    Gray = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3,
    Orange = 4,
    Red = 5,
    Purple = 6,
}
