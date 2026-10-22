namespace TodoApp.Features.Users;

public sealed record UserSummary(string Id, string DisplayName)
{
    public string Initials =>
        string.Concat(DisplayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));

    public static string ToDisplayName(string? firstName, string? lastName, string? email)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return fullName.Length > 0 ? fullName : email ?? "Utilisateur inconnu";
    }
}
