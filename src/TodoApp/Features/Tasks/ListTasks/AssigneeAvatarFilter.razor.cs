using System.Collections.Frozen;
using Microsoft.AspNetCore.Components;
using TodoApp.Features.Users;

namespace TodoApp.Features.Tasks.ListTasks;

/// <summary>
/// Filtre « Assigné » façon Jira : pile d'avatars cliquables (bascule),
/// « +N » ouvre les utilisateurs restants et l'option « Non assignée ».
/// </summary>
public sealed partial class AssigneeAvatarFilter
{
    [Parameter, EditorRequired] public IReadOnlyList<UserSummary> Users { get; set; } = [];

    [Parameter, EditorRequired] public IReadOnlySet<string> SelectedIds { get; set; } = FrozenSet<string>.Empty;

    [Parameter] public bool IncludeUnassigned { get; set; }

    [Parameter] public string? CurrentUserId { get; set; }

    [Parameter] public int MaxVisible { get; set; } = 5;

    [Parameter] public EventCallback<string> OnToggle { get; set; }

    [Parameter] public EventCallback OnToggleUnassigned { get; set; }

    [Parameter] public RenderFragment? Trigger { get; set; }

    private IEnumerable<UserSummary> Visible => Users.Take(MaxVisible);

    private IEnumerable<UserSummary> Hidden => Users.Skip(MaxVisible);

    private int HiddenCount => Math.Max(0, Users.Count - MaxVisible);

    /// <summary>Badge du « +N » : sélections invisibles dans la pile.</summary>
    private int HiddenActiveCount =>
        Hidden.Count(u => SelectedIds.Contains(u.Id)) + (IncludeUnassigned ? 1 : 0);

    private string LabelFor(UserSummary user) =>
        string.Equals(user.Id, CurrentUserId, StringComparison.Ordinal)
            ? $"{user.DisplayName} (moi)"
            : user.DisplayName;
}
