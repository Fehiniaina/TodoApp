namespace TodoApp.Features.Shared.Time;

/// <summary>
/// Fuseau de l'utilisateur pour le circuit courant.
/// Valeur par défaut issue de la configuration, remplacée par celle du navigateur dès qu'elle est connue.
/// </summary>
internal sealed class UserTimeZone(IConfiguration configuration)
{
    public TimeZoneInfo TimeZone { get; private set; } =
        Resolve(configuration["App:DefaultTimeZone"]) ?? TimeZoneInfo.Utc;

    public bool IsDetected { get; private set; }

    public bool TrySet(string? ianaId)
    {
        if (Resolve(ianaId) is not { } timeZone) return false;

        TimeZone = timeZone;
        IsDetected = true;
        return true;
    }

    private static TimeZoneInfo? Resolve(string? id) =>
        !string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz) ? tz : null;
}