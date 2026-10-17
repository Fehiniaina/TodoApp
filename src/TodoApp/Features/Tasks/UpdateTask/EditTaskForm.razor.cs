using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using TodoApp.Common;
using TodoApp.Features.Shared.Logging;
using TodoApp.Features.Shared.Notifications;
using TodoApp.Features.Tasks.CreateTask;
using TodoApp.Features.Tasks.ListTasks;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.UpdateTask;

public partial class EditTaskForm
{
    [Inject] private UpdateTaskHandler Handler { get; set; } = null!;
    [Inject] private NotificationService Notification { get; set; } = null!;
    [Inject] private ILogger<EditTaskForm> Logger { get; set; } = null!;

    // Ne pas nommer ce paramètre "Task" : il masquerait System.Threading.Tasks.Task dans la classe.
    [Parameter, EditorRequired] public TaskSummary TaskToEdit { get; set; } = null!;
    [Parameter] public EventCallback OnSaved { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private TaskFormModel Model { get; set; } = new();
    private bool IsSubmitting { get; set; }
    private string? ErrorMessage { get; set; }

    protected override void OnInitialized() =>
        Model = new TaskFormModel
        {
            Title = TaskToEdit.Title,
            Description = TaskToEdit.Description,
            Priority = TaskToEdit.Priority,
        };

    private async Task SubmitAsync()
    {
        if (IsSubmitting) return;

        IsSubmitting = true;
        ErrorMessage = null;

        try
        {
            var result = await Handler.HandleAsync(
                new UpdateTaskCommand(TaskToEdit.Id, Model.Title, Model.Description, Model.Priority));

            if (result.IsFailure)
            {
                switch (result.Error.Type)
                {
                    case ErrorType.Validation:
                        ErrorMessage = result.Error.Message;
                        Notification.Warning(result.Error.Message, "Formulaire incomplet");
                        break;

                    case ErrorType.NotFound:
                        Notification.Warning(result.Error.Message, "Tâche introuvable");
                        await OnCancel.InvokeAsync(); // la tâche n'existe plus : on ferme
                        break;

                    default:
                        Logger.OperationFailed(OperationsTask.UpdateTask, result.Error.Code, result.Error.Message);
                        Notification.Error(result.Error.Message, "Modification impossible");
                        break;
                }

                return;
            }

            Notification.Success($"« {Model.Title.Trim()} » a été modifiée.", "Tâche modifiée");
            await OnSaved.InvokeAsync();
        }
        catch (Exception ex)
        {
            Logger.UnexpectedError(OperationsTask.UpdateTask, ex);
            Notification.Error("Une erreur inattendue est survenue. Réessayez plus tard.", "Erreur");
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}