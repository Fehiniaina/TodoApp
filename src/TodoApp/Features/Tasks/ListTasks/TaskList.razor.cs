using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TodoApp.Common;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Shared.Logging;
using TodoApp.Features.Shared.Notifications;
using TodoApp.Features.Shared.Time;
using TodoApp.Features.Tasks.ChangeTaskStatus;
using TodoApp.Features.Tasks.CreateTask;
using TodoApp.Features.Tasks.DeleteTasks;
using TodoApp.Features.Tasks.Recurrence;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.ListTasks;

public sealed partial class TaskList : IDisposable
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    [Inject] private ListTasksHandler Handler { get; set; } = null!;
    [Inject] private ChangeTaskStatusHandler ChangeStatusHandler { get; set; } = null!;
    [Inject] private DeleteTasksHandler DeleteTasksHandler { get; set; } = null!;
    [Inject] private StopRecurringTaskHandler StopRecurringHandler { get; set; } = null!;
    [Inject] private TaskChangeNotifier Notifier { get; set; } = null!;
    [Inject] private NotificationService Notification { get; set; } = null!;
    [Inject] private IUserClock Clock { get; set; } = null!;
    [Inject] private UserTimeZone UserTimeZone { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private ILogger<TaskList> Logger { get; set; } = null!;
    [Inject] private ICurrentUser CurrentUser { get; set; } = null!;

    [CascadingParameter] private Task<AuthenticationState> AuthenticationState { get; set; } = null!;

    private PagedResult<TaskSummary>? _result;
    private int _currentPage = 1;
    private int _pageSize = 10;
    private bool _isLoading = true;
    private string? _errorMessage;
    private string? _createdById;
    private DateOnly _today;

    private readonly HashSet<Guid> _changingIds = [];

    private bool _showCreateModal;
    private TaskSummary? _editingTask;
    private bool _focusChecklist;

    private TaskScope _scope = TaskScope.All;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthenticationState;
        _createdById = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _today = Clock.Today;

        Notifier.Changed += OnTasksChanged;

        await LoadTasksAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || UserTimeZone.IsDetected) return;

        try
        {
            await using var module = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./Features/Tasks/ListTasks/TaskList.razor.js");

            if (UserTimeZone.TrySet(await module.InvokeAsync<string>("getTimeZone")))
            {
                _today = Clock.Today;
                StateHasChanged();
            }
        }
        catch (JSException ex)
        {
            LogTimeZoneDetectionFailed(Logger, ex);
        }
        catch (JSDisconnectedException)
        {
            // Circuit fermé pendant l'appel : rien à faire.
        }
    }

    public void Dispose() => Notifier.Changed -= OnTasksChanged;

    private async Task LoadTasksAsync()
    {
        if (string.IsNullOrEmpty(_createdById))
        {
            _errorMessage = "Impossible d'identifier l'utilisateur courant.";
            _isLoading = false;
            return;
        }

        _isLoading = true;
        var result = await Handler.HandleAsync(new ListTasksQuery(_scope, Page: _currentPage, PageSize: _pageSize));
        _isLoading = false;

        if (result.IsFailure)
        {
            _errorMessage = result.Error.Message;
            return;
        }

        _result = result.Value;

        if (_result.Items.Count == 0 && _result.TotalCount > 0)
        {
            _currentPage = _result.TotalPages;
            await LoadTasksAsync();
        }
    }

    private Task OnPageChangedAsync(int page)
    {
        _currentPage = page;
        return LoadTasksAsync();
    }

    private Task OnPageSizeChangedAsync(int pageSize)
    {
        _pageSize = pageSize;
        _currentPage = 1;
        return LoadTasksAsync();
    }
    private void OnTasksChanged(object? sender, EventArgs e) =>
        _ = InvokeAsync(async () =>
        {
            try
            {
                await LoadTasksAsync();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        });

    private void OpenCreateModal() => _showCreateModal = true;
    private void CloseCreateModal() => _showCreateModal = false;

    private async Task OnTaskCreatedAsync()
    {
        _showCreateModal = false;
        _currentPage = 1;
        await LoadTasksAsync();
    }

    private void OpenEditModal(TaskSummary task)
    {
        _editingTask = task;
        _focusChecklist = false;
    }

    private void OpenChecklist(TaskSummary task)
    {
        _editingTask = task;
        _focusChecklist = true;
    }

    private void CloseEditModal() => _editingTask = null;

    private async Task OnStatusSelectedAsync(TaskSummary task, ChangeEventArgs e)
    {
        if (Enum.TryParse<TaskItemStatus>(e.Value?.ToString(), out var newStatus) && newStatus != task.Status)
        {
            await ChangeStatusAsync(task, newStatus);
        }
    }

    private async Task ChangeStatusAsync(TaskSummary original, TaskItemStatus newStatus)
    {
        if (!_changingIds.Add(original.Id)) return;

        ReplaceItem(original with { Status = newStatus });

        try
        {
            var result = await ChangeStatusHandler.HandleAsync(new ChangeTaskStatusCommand(original.Id, newStatus));

            if (result.IsFailure)
            {
                ReplaceItem(original);
                Notification.Warning(result.Error.Message, "Changement de statut impossible");
                return;
            }

            ReplaceItem(original with
            {
                Status = result.Value.Status,
                CompletedAt = result.Value.CompletedAt,
            });
        }
        catch (Exception ex)
        {
            ReplaceItem(original);
            Logger.UnexpectedError(OperationsTask.ChangeTaskStatus, ex);
            Notification.Error("Une erreur inattendue est survenue. Réessayez plus tard.", "Erreur");
        }
        finally
        {
            _changingIds.Remove(original.Id);
        }
    }

    /// <summary>Remplace une seule ligne sans recharger la page.</summary>
    private void ReplaceItem(TaskSummary updated)
    {
        if (_result is null) return;

        _result = _result with
        {
            Items = _result.Items.Select(t => t.Id == updated.Id ? updated : t).ToList(),
        };
    }

    private async Task MoveToTrashAsync(TaskSummary task)
    {
        try
        {
            var result = await DeleteTasksHandler.HandleAsync(new DeleteTasksCommand(task.Id));

            if (result.IsFailure)
            {
                Logger.OperationFailed(OperationsTask.TrashTask, result.Error.Code, result.Error.Message);
                Notification.Warning(result.Error.Message, "Suppression impossible");
                return;
            }

            Notification.Info($"« {task.Title} » a été déplacée dans la corbeille.", "Tâche supprimée");
        }
        catch (Exception ex)
        {
            Logger.UnexpectedError(OperationsTask.TrashTask, ex);
            Notification.Error("Une erreur inattendue est survenue. Réessayez plus tard.", "Erreur");
        }
    }

    private async Task StopRecurrenceAsync(Guid seriesId)
    {
        var result = await StopRecurringHandler.HandleAsync(seriesId);

        if (result.IsFailure)
        {
            Notification.Warning(result.Error.Message, "Récurrence");
            return;
        }

        Notification.Info("Plus aucune nouvelle occurrence ne sera créée.", "Récurrence arrêtée");
    }

    private string FormatDate(DateTime utc) => Clock.ToLocal(utc).ToString("d MMM yyyy", French);

    private string FormatDateTime(DateTime utc) => Clock.ToLocal(utc).ToString("d MMM yyyy 'à' HH:mm", French);

    private static readonly (TaskScope Scope, string Label)[] Scopes =
    [
        (TaskScope.All, "Toutes"),
        (TaskScope.AssignedToMe, "Assignées à moi"),
        (TaskScope.CreatedByMe, "Créées par moi"),
    ];

    private Task ChangeScopeAsync(TaskScope scope)
    {
        if (scope == _scope) return Task.CompletedTask;

        _scope = scope;
        _currentPage = 1;
        return LoadTasksAsync();
    }

    private string? StatusTooltip(TaskSummary task) =>
        task.CompletedAt is { } completedAt ? $"Résolue le {FormatDateTime(completedAt)}" : null;

    [LoggerMessage(Level = LogLevel.Error, Message =$"Browser time zone detection failed; using the default time zone")]
    private static partial void LogTimeZoneDetectionFailed(ILogger logger, Exception ex);
}
