using System.Globalization;
using Microsoft.AspNetCore.Components;
using TodoApp.Common;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Shared.Notifications;
using TodoApp.Features.Tasks.AssignTask;
using TodoApp.Features.Tasks.ChangeTaskStatus;
using TodoApp.Features.Tasks.UpdateTask;
using TodoApp.Features.Users;

namespace TodoApp.Features.Tasks.Details;

/// <summary>Panneau de droite : statut, assigné, priorité, échéance.</summary>
public sealed partial class TaskDetailsPage
{
    private const string DateFormat = "yyyy-MM-dd"; // format imposé par <input type="date">

    [Inject] private ChangeTaskStatusHandler StatusHandler { get; set; } = null!;
    [Inject] private AssignTaskHandler AssignHandler { get; set; } = null!;
    [Inject] private GetAssignableUsersHandler AssignableUsersHandler { get; set; } = null!;
    [Inject] private ICurrentUser CurrentUser { get; set; } = null!;
    [Inject] private NotificationService Toasts { get; set; } = null!;

    private IReadOnlyList<UserSummary> _assignableUsers = [];
    private string _currentUserId = string.Empty;
    private bool _isSavingField;

    private IEnumerable<TaskItemStatus> StatusOptions =>
        [_task!.Status, .. TaskWorkflow.NextStatuses(_task.Status)];

    private static TaskPriority[] PriorityOptions { get; } = Enum.GetValues<TaskPriority>();

    private string? DueDateValue => _task!.DueDate?.ToString(DateFormat, CultureInfo.InvariantCulture);

    private async Task LoadFieldOptionsAsync()
    {
        _currentUserId = await CurrentUser.GetIdAsync();
        _assignableUsers = await AssignableUsersHandler.HandleAsync();
    }

    private Task OnStatusChangedAsync(ChangeEventArgs e)
    {
        if (!Enum.TryParse<TaskItemStatus>(e.Value?.ToString(), out var status) || status == _task!.Status)
        {
            return Task.CompletedTask;
        }

        var taskId = _task.Id;
        return SaveFieldAsync(
            async () => ErrorOf(await StatusHandler.HandleAsync(new ChangeTaskStatusCommand(taskId, status))),
            "Changement de statut impossible");
    }
    private Task OnAssigneeChangedAsync(ChangeEventArgs e)
    {
        var assigneeId = e.Value?.ToString() is { Length: > 0 } id ? id : null;
        return string.Equals(assigneeId, _task!.AssigneeId, StringComparison.Ordinal)
            ? Task.CompletedTask
            : AssignAsync(assigneeId);
    }

    private Task AssignToMeAsync() => AssignAsync(_currentUserId);

    private Task AssignAsync(string? assigneeId)
    {
        var taskId = _task!.Id;
        return SaveFieldAsync(
            async () => ErrorOf(await AssignHandler.HandleAsync(new AssignTaskCommand(taskId, assigneeId))),
            "Assignation impossible");
    }

    private Task OnPriorityChangedAsync(ChangeEventArgs e)
    {
        if (!Enum.TryParse<TaskPriority>(e.Value?.ToString(), out var priority) || priority == _task!.Priority)
        {
            return Task.CompletedTask;
        }

        return SaveFieldAsync(
            () => UpdateTaskAsync(t => new UpdateTaskCommand(t.Id, t.Title, t.Description, priority, t.DueDate)),
            "Changement de priorité impossible");
    }

    private Task OnDueDateChangedAsync(ChangeEventArgs e)
    {
        // Champ vidé dans le sélecteur natif : chaîne vide → pas d'échéance.
        DateOnly? dueDate = DateOnly.TryParseExact(e.Value?.ToString(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

        return dueDate == _task!.DueDate ? Task.CompletedTask : SaveDueDateAsync(dueDate);
    }

    private Task ClearDueDateAsync() => SaveDueDateAsync(null);

    private Task SaveDueDateAsync(DateOnly? dueDate) =>
        SaveFieldAsync(
            () => UpdateTaskAsync(t => new UpdateTaskCommand(t.Id, t.Title, t.Description, t.Priority, dueDate)),
            "Changement d'échéance impossible");

    private async Task SaveFieldAsync(Func<Task<string?>> save, string errorTitle)
    {
        _isSavingField = true;
        try
        {
            if (await save() is { } error)
            {
                Toasts.Warning(error, errorTitle);
            }
        }
        finally
        {
            _isSavingField = false;
        }

        await ReloadAsync();
    }

    private static string? ErrorOf(Result result) => result.IsFailure ? result.Error.Message : null;

    private static string? ErrorOf<T>(Result<T> result) => result.IsFailure ? result.Error.Message : null;
}
