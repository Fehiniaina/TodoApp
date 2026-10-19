using Microsoft.AspNetCore.Components;
using TodoApp.Common;
using TodoApp.Domain.Tasks.Checklist;
using TodoApp.Features.Shared.Logging;
using TodoApp.Features.Shared.Notifications;
using TodoApp.Features.Tasks.CreateTask;

namespace TodoApp.Features.Tasks.Checklist;

public partial class ChecklistEditor
{
    [Inject] private ChecklistHandler Handler { get; set; } = null!;
    [Inject] private NotificationService Notification { get; set; } = null!;
    [Inject] private ILogger<ChecklistEditor> Logger { get; set; } = null!;

    [Parameter, EditorRequired] public Guid TaskId { get; set; }

    private IReadOnlyList<ChecklistItemDto> _items = [];
    private string? _newTitle;
    private bool _busy;

    private int Done => _items.Count(i => i.IsDone);
    private int Percent => _items.Count == 0 ? 0 : Done * 100 / _items.Count;

    protected override async Task OnParametersSetAsync() => _items = await Handler.GetAsync(TaskId);

    private async Task AddAsync()
    {
        var title = _newTitle;
        if (await RunAsync(() => Handler.AddAsync(TaskId, title ?? string.Empty)))
        {
            _newTitle = null;
        }
    }

    private Task<bool> SetDoneAsync(ChecklistItemDto item, bool isDone) =>
        RunAsync(() => Handler.SetDoneAsync(TaskId, item.Id, isDone));

    private Task<bool> RemoveAsync(ChecklistItemDto item) =>
        RunAsync(() => Handler.RemoveAsync(TaskId, item.Id));

    private async Task<bool> RunAsync(Func<Task<Result<IReadOnlyList<ChecklistItemDto>>>> action)
    {
        if (_busy)
        {
            return false;
        }

        _busy = true;

        try
        {
            var result = await action();

            if (result.IsFailure)
            {
                Notification.Warning(result.Error.Message, "Checklist");
                _items = await Handler.GetAsync(TaskId);   // resynchronise l'affichage (case cochée, etc.)
                return false;
            }

            _items = result.Value;
            return true;
        }
        catch (Exception ex)
        {
            Logger.UnexpectedError(OperationsTask.UpdateChecklist, ex);
            Notification.Error("Une erreur inattendue est survenue. Réessayez plus tard.", "Erreur");
            return false;
        }
        finally
        {
            _busy = false;
        }
    }
}