using System.Globalization;

namespace TodoApp.Features.Shared.Formatting;

internal static class FileSize
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public static string Format(long bytes) => bytes switch
    {
        < 1024 => string.Create(French, $"{bytes} octets"),
        < 1024 * 1024 => string.Create(French, $"{bytes / 1024d:0} Ko"),
        _ => string.Create(French, $"{bytes / (1024d * 1024):0.#} Mo"),
    };
}
