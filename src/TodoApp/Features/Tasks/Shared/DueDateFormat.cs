using System.Globalization;

namespace TodoApp.Features.Tasks.Shared;

/// <summary>
/// Format d'affichage d'une échéance : l'année n'apparaît que si elle diffère de l'année en cours,
/// pour lever l'ambiguïté (« 27 déc. » fin octobre = l'an dernier ou cette année ?).
/// </summary>
internal static class DueDateFormat
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    /// <param name="today">Jour courant dans le fuseau de l'utilisateur (IUserClock.Today).</param>
    public static string Short(DateOnly dueDate, DateOnly today) =>
        dueDate.Year == today.Year
            ? dueDate.ToString("d MMM", French)
            : dueDate.ToString("d MMM yyyy", French);
}
