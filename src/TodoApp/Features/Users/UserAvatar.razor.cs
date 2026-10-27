using Microsoft.AspNetCore.Components;

namespace TodoApp.Features.Users;

public sealed partial class UserAvatar
{
    private static readonly string[] Palette =
    ["#0c66e4", "#6e5dc6", "#1f845a", "#c25100", "#ae2e24", "#206a83", "#943d73"];

    [Parameter] public string? UserId { get; set; }
    [Parameter] public string? Name { get; set; }

    [Parameter] public bool Decorative { get; set; }

    private string Initials => new UserSummary(UserId ?? string.Empty, Name ?? string.Empty).Initials;

    private string Color => Palette[(UserId ?? string.Empty).Sum(c => c) % Palette.Length];
}
