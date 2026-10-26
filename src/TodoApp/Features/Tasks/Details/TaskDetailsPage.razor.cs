using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TodoApp.Features.Shared.Time;
using TodoApp.Features.Tasks.UpdateTask;

namespace TodoApp.Features.Tasks.Details;

public sealed partial class TaskDetailsPage
{
    [Inject] private UpdateTaskHandler UpdateHandler { get; set; } = null!;

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    private int _activityVersion;

    private static readonly (ActivityTab Value, string Label)[] Tabs =
    [
        (ActivityTab.All, "Tout"),
        (ActivityTab.Comments, "Commentaires"),
        (ActivityTab.History, "Historique"),
    ];

    [Parameter] public Guid TaskId { get; set; }

    [Inject] private GetTaskDetailsHandler Handler { get; set; } = null!;
    [Inject] private IUserClock Clock { get; set; } = null!;

    [CascadingParameter] private Task<AuthenticationState> AuthenticationState { get; set; } = null!;

    private TaskDetails? _task;
    private bool _notFound;
    private DateOnly _today;
    private ActivityTab _activeTab = ActivityTab.All;
    private string CurrentUserInitials { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthenticationState).User;
        CurrentUserInitials = Initials(user.Identity?.Name ?? string.Empty);
    }

    protected override async Task OnParametersSetAsync()
    {
        _today = Clock.Today;

        if (_task?.Id == TaskId)
        {
            return;
        }

        _task = null;
        _notFound = false;

        var result = await Handler.HandleAsync(TaskId);
        if (result.IsFailure)
        {
            _notFound = true;
            return;
        }

        _task = result.Value;
    }

    private static string Initials(string name) =>
        string.Concat(name
            .Split([' ', '@', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));

    private void OnAttachmentsChanged() => _activityVersion++;

    private Task<string?> SaveTitleAsync(string? title) =>
    UpdateContentAsync(title!, _task!.Description);

    private Task<string?> SaveDescriptionAsync(string? description) =>
        UpdateContentAsync(_task!.Title, description);

    private async Task<string?> UpdateContentAsync(string title, string? description)
    {
        var task = _task!;
        var result = await UpdateHandler.HandleAsync(
            new UpdateTaskCommand(task.Id, title, description, task.Priority, task.DueDate));

        if (result.IsFailure)
        {
            return result.Error.Message;
        }

        await ReloadAsync();
        return null;
    }

    /// <summary>Recharge la tâche et l'historique après une modification.</summary>
    private async Task ReloadAsync()
    {
        var result = await Handler.HandleAsync(TaskId);
        if (result.IsSuccess)
        {
            _task = result.Value;
        }

        _activityVersion++;
        StateHasChanged();
    }
}
