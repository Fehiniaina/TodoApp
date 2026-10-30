using System.Globalization;

namespace TodoApp.Domain.Common;

/// <summary>
/// Forme texte d'un enum dans les données stockées (historique, et plus tard outbox, index).
/// On stocke la VALEUR numérique : un renommage de membre ne casse rien.
/// Chaque enum stocké ainsi DOIT avoir des valeurs explicites et un test de contrat.
/// </summary>
internal static class EnumStorage
{
    public static string ToStored<TEnum>(TEnum value) where TEnum : struct, Enum =>
        Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);

    public static bool TryFromStored<TEnum>(string? stored, out TEnum value) where TEnum : struct, Enum
    {
        if (int.TryParse(stored, NumberStyles.None, CultureInfo.InvariantCulture, out var raw))
        {
            value = (TEnum)Enum.ToObject(typeof(TEnum), raw);
            if (Enum.IsDefined(value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }
}
