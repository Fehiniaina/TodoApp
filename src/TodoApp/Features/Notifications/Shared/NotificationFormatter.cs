using System.Globalization;
using System.Runtime.CompilerServices;
using TodoApp.Domain.Notifications;

namespace TodoApp.Features.Notifications.Shared;

internal static class NotificationFormatter
{
    public static string Action(NotificationType type) => type switch
    {
        NotificationType.TaskAssigned => "vous a assigné",
        _ => "a mis à jour",
    };

    public static string Ago(DateTime createdAtUtc, DateTime nowUtc)
    {
        var elapsed = nowUtc - createdAtUtc;

        return elapsed switch
        {
            { TotalMinutes: < 1 } => "À l'instant",
            { TotalHours: < 1 } => Invariant($"Il y a {(int)elapsed.TotalMinutes} min"),
            { TotalDays: < 1 } => Invariant($"Il y a {(int)elapsed.TotalHours} h"),
            { TotalDays: < 2 } => "Hier",
            { TotalDays: < 7 } => Invariant($"Il y a {(int)elapsed.TotalDays} j"),
            _ => createdAtUtc.ToString("d MMM", CultureInfo.GetCultureInfo("fr-FR")),
        };
    }

    public static string Initials(string displayName) =>
        string.Concat(displayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));

    private static string Invariant(FormattableString value) =>
        FormattableString.Invariant(value);
}
