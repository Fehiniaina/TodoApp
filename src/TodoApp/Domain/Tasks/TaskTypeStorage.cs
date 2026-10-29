using System.Globalization;

namespace TodoApp.Domain.Tasks;

/// <summary>
/// Forme texte d'un TaskType dans les données stockées (historique, et plus tard outbox, index).
/// On stocke la VALEUR numérique, déjà protégée par TaskTypeContractTests :
/// un renommage de membre ne casse donc rien.
/// </summary>
internal static class TaskTypeStorage
{
    public static string ToStored(TaskType type) =>
        ((int)type).ToString(CultureInfo.InvariantCulture);

    public static bool TryFromStored(string? stored, out TaskType type)
    {
        if (int.TryParse(stored, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            && Enum.IsDefined((TaskType)value))
        {
            type = (TaskType)value;
            return true;
        }

        type = default;
        return false;
    }
}
