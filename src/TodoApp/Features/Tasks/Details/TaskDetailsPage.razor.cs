using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TodoApp.Features.Shared.Time;

namespace TodoApp.Features.Tasks.Details;

public sealed partial class TaskDetailsPage
{
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
}
