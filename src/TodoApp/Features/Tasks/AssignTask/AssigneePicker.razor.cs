using Microsoft.AspNetCore.Components;
using TodoApp.Features.Shared;
using TodoApp.Features.Shared.Notifications;
using TodoApp.Features.Users;

namespace TodoApp.Features.Tasks.AssignTask;

public partial class AssigneePicker
{
    [Inject] private AssignTaskHandler AssignHandler { get; set; } = null!;
    [Inject] private GetAssignableUsersHandler UsersHandler { get; set; } = null!;
    [Inject] private ICurrentUser CurrentUser { get; set; } = null!;
    [Inject] private NotificationService Notification { get; set; } = null!;

    [Parameter, EditorRequired] public Guid TaskId { get; set; }
    [Parameter] public string? AssigneeId { get; set; }

    private IReadOnlyList<UserSummary> _users = [];
    private string? _assigneeId;
    private string? _currentUserId;
    private bool _busy;

    protected override async Task OnInitializedAsync()
    {
        _assigneeId = AssigneeId;
        _currentUserId = await CurrentUser.GetIdAsync();
        _users = await UsersHandler.HandleAsync();
    }

    private Task OnChangedAsync(ChangeEventArgs e) =>
        AssignAsync(string.IsNullOrEmpty(e.Value?.ToString()) ? null : e.Value!.ToString());

    private Task AssignToMeAsync() => AssignAsync(_currentUserId);

    private async Task AssignAsync(string? assigneeId)
    {
        if (
            (_busy)
            || string.Equals(assigneeId, _assigneeId, StringComparison.Ordinal))
        {
            return;
        }

        _busy = true;

        var previous = _assigneeId;
        _assigneeId = assigneeId;

        try
        {
            var result = await AssignHandler.HandleAsync(new AssignTaskCommand(TaskId, assigneeId));

            if (result.IsFailure)
            {
                _assigneeId = previous;
                Notification.Warning(result.Error.Message, "Assignation impossible");
                return;
            }

            var name = _users.FirstOrDefault(u => string.Equals(u.Id, assigneeId, StringComparison.Ordinal))?.DisplayName;
            Notification.Success(name is null ? "La tâche n'est plus assignée." : $"Tâche assignée à {name}.");
        }
        finally
        {
            _busy = false;
        }
    }
}
