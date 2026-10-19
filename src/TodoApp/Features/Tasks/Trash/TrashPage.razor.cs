using System.Globalization;
using Microsoft.AspNetCore.Components;
using TodoApp.Features.Shared.Logging;
using TodoApp.Features.Shared.Notifications;
using TodoApp.Features.Shared.Time;
using TodoApp.Features.Tasks.CreateTask;

namespace TodoApp.Features.Tasks.Trash;

public partial class TrashPage
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    [Inject] private ListTrashedTasksHandler ListHandler { get; set; } = null!;
    [Inject] private RestoreTaskHandler RestoreHandler { get; set; } = null!;
    [Inject] private PurgeTaskHandler PurgeHandler { get; set; } = null!;
    [Inject] private NotificationService Notification { get; set; } = null!;
    [Inject] private IUserClock Clock { get; set; } = null!;
    [Inject] private ILogger<TrashPage> Logger { get; set; } = null!;

    private IReadOnlyList<TrashedTaskSummary>? _items;
    private Guid? _pendingPurgeId;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        var result = await ListHandler.HandleAsync();
        _items = result.IsSuccess ? result.Value : [];
    }

    private async Task RestoreAsync(TrashedTaskSummary task)
    {
        var result = await RestoreHandler.HandleAsync(new RestoreTaskCommand(task.Id));

        if (result.IsFailure)
        {
            Logger.OperationFailed(OperationsTask.RestoreTask, result.Error.Code, result.Error.Message);
            Notification.Warning(result.Error.Message, "Restauration impossible");
        }
        else
        {
            Notification.Success($"« {task.Title} » a été restaurée.", "Tâche restaurée");
        }

        await LoadAsync();
    }

    private async Task PurgeAsync(TrashedTaskSummary task)
    {
        _pendingPurgeId = null;
        var result = await PurgeHandler.HandleAsync(new PurgeTaskCommand(task.Id));

        if (result.IsFailure)
        {
            Logger.OperationFailed(OperationsTask.PurgeTask, result.Error.Code, result.Error.Message);
            Notification.Warning(result.Error.Message, "Suppression impossible");
        }
        else
        {
            Notification.Info($"« {task.Title} » a été supprimée définitivement.");
        }

        await LoadAsync();
    }

    private string FormatDate(DateTime utc) => Clock.ToLocal(utc).ToString("d MMM yyyy 'à' HH:mm", French);
}