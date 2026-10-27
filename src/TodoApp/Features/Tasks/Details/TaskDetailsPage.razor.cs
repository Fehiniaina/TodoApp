using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TodoApp.Features.Shared.Time;
using TodoApp.Features.Tasks.UpdateTask;

namespace TodoApp.Features.Tasks.Details;

public sealed partial class TaskDetailsPage
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private static readonly (ActivityTab Value, string Label)[] Tabs =
    [
        (ActivityTab.All, "Tout"),
        (ActivityTab.Comments, "Commentaires"),
        (ActivityTab.History, "Historique"),
    ];

    [Parameter] public Guid TaskId { get; set; }

    [Inject] private GetTaskDetailsHandler Handler { get; set; } = null!;
    [Inject] private UpdateTaskHandler UpdateHandler { get; set; } = null!;
    [Inject] private IUserClock Clock { get; set; } = null!;

    [CascadingParameter] private Task<AuthenticationState> AuthenticationState { get; set; } = null!;

    private TaskDetails? _task;
    private bool _notFound;
    private DateOnly _today;
    private ActivityTab _activeTab = ActivityTab.All;

    private int _activityVersion;
    private int _fieldsVersion;

    private string CurrentUserInitials { get; set; } = string.Empty;

    private enum ActivityTab
    {
        All,
        Comments,
        History,
    }

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthenticationState).User;
        CurrentUserInitials = Initials(user.Identity?.Name ?? string.Empty);

        await LoadFieldOptionsAsync(); // défini dans TaskDetailsPage.Fields.cs
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

    /// <summary>Recharge la tâche, l'historique et les champs après une modification.</summary>
    private async Task ReloadAsync()
    {
        var result = await Handler.HandleAsync(TaskId);
        if (result.IsSuccess)
        {
            _task = result.Value;
        }

        _activityVersion++;
        _fieldsVersion++;
        StateHasChanged();
    }

    private async Task<string?> SaveTitleAsync(string? title) =>
        await SaveContentAsync(t => new UpdateTaskCommand(t.Id, title!, t.Description, t.Priority, t.DueDate));

    private async Task<string?> SaveDescriptionAsync(string? description) =>
        await SaveContentAsync(t => new UpdateTaskCommand(t.Id, t.Title, description, t.Priority, t.DueDate));

    /// <summary>Édition sur place (titre, description) : l'erreur est affichée sous le champ.</summary>
    private async Task<string?> SaveContentAsync(Func<TaskDetails, UpdateTaskCommand> change)
    {
        var error = await UpdateTaskAsync(change);
        if (error is null)
        {
            await ReloadAsync();
        }

        return error;
    }

    /// <summary>Repart de la tâche actuelle et ne change que ce que <paramref name="change"/> modifie.</summary>
    private async Task<string?> UpdateTaskAsync(Func<TaskDetails, UpdateTaskCommand> change)
    {
        var result = await UpdateHandler.HandleAsync(change(_task!));
        return result.IsFailure ? result.Error.Message : null;
    }

    // Le compteur, l'historique et les champs suivent le même chemin que les autres modifications.
    private Task OnAttachmentsChanged() => ReloadAsync();

    private static string Initials(string name) =>
        string.Concat(name
            .Split([' ', '@', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));
}
