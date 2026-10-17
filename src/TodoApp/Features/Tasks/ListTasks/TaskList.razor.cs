using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TodoApp.Common;
using TodoApp.Features.Shared;
using TodoApp.Features.Shared.Logging;
using TodoApp.Features.Shared.Notifications;
using TodoApp.Features.Tasks.CreateTask;
using TodoApp.Features.Tasks.DeleteTasks;

namespace TodoApp.Features.Tasks.ListTasks;

public partial class TaskList : IDisposable
{
    [Parameter] public EventCallback<TaskSummary> OnEdit { get; set; }

    private Guid? _pendingDeleteId;
    private bool _isDeleting;

    private void RequestDelete(Guid id) => _pendingDeleteId = id;
    private void CancelDelete() => _pendingDeleteId = null;

    [Inject] private ListTasksHandler Handler { get; set; } = null!;
    [Inject] private TaskChangeNotifier Notifier { get; set; } = null!;
    [Inject] private DeleteTasksHandler DeleteTasksHandler { get; set; } = null!;
    [Inject] private NotificationService Notification { get; set; } = null!;
    [Inject] private ILogger<TaskList> Logger { get; set; } = null!;

    [CascadingParameter] private Task<AuthenticationState> AuthenticationState { get; set; } = null!;

    private bool _showCreateModal;
    private void OpenCreateModal() => _showCreateModal = true;
    private void CloseCreateModal() => _showCreateModal = false;


    private PagedResult<TaskSummary>? _result;
    private int _currentPage = 1;
    private int _pageSize = 10;
    private bool _isLoading = true;
    private string? _errorMessage;
    private string? _ownerId;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthenticationState;
        _ownerId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

        Notifier.Changed += OnTasksChanged;

        await LoadTasksAsync();
    }

    private async Task LoadTasksAsync()
    {
        if (string.IsNullOrEmpty(_ownerId))
        {
            _errorMessage = "Unable to identify the current user.";
            _isLoading = false;
            return;
        }

        _isLoading = true;
        var result = await Handler.HandleAsync(new ListTasksQuery(_ownerId, Page: _currentPage, PageSize: _pageSize));
        _isLoading = false;

        if (!result.IsSuccess)
        {
            _errorMessage = result.Error.Message;
            return;
        }

        _result = result.Value;

        // La page courante peut ne plus exister (par exemple après la suppression de la dernière tâche de la page)
        if (_result.Items.Count == 0 && _result.TotalCount > 0)
        {
            _currentPage = _result.TotalPages;
            await LoadTasksAsync();
        }
    }

    private async Task OnTaskCreatedAsync()
    {
        _showCreateModal = false;
        _currentPage = 1;          // la nouvelle tâche apparaît en tête si le tri est par date desc
        await LoadTasksAsync();    // adaptez au nom de votre méthode de chargement
    }

    private async Task OnPageChangedAsync(int page)
    {
        _currentPage = page;
        await LoadTasksAsync();
    }

    private async Task OnPageSizeChangedAsync(int pageSize)
    {
        _pageSize = pageSize;
        _currentPage = 1;   // revenir à la première page
        await LoadTasksAsync();
    }

    // Called by TaskChangeNotifier on a background thread — must marshal to the circuit thread.
    private void OnTasksChanged() =>
        InvokeAsync(async () =>
        {
            await LoadTasksAsync(); // recharge la page courante, sans revenir à la première
            StateHasChanged();
        });

    public async Task ConfirmDeleteAsync(Guid id)
    {
        if (string.IsNullOrEmpty(_ownerId))
        {
            Notification.Error("Impossible d'identifier l'utilisateur courant.", "Suppression impossible");
            return;
        }

        _isDeleting = true;

        var title = _result?.Items.FirstOrDefault(t => t.Id == id)?.Title ?? "La tâche";

        try
        {
            var result = await DeleteTasksHandler.HandleAsync(new DeleteTasksCommand(id, _ownerId));

            if (result.IsFailure)
            {
                Logger.OperationFailed(OperationsTask.DeleteTask, result.Error.Code, result.Error.Message);
                Notification.Error(result.Error.Message, "Suppression impossible");
                return;
            }

            Notification.Success($"« {title} » a été supprime avec success.", "Tache supprimée");
        }
        catch(Exception ex)
        {
            Logger.UnexpectedError(OperationsTask.DeleteTask, ex);
            Notification.Error("Une erreur inattendue est survenue. Réessayez plus tard.", "Erreur");
        }
        finally
        {
            _isDeleting = false;
            _pendingDeleteId = null;
        }
    }


    public void Dispose() => Notifier.Changed -= OnTasksChanged;
}
