using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using TodoApp.Common;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Recurrence;
using TodoApp.Features.Shared.Logging;
using TodoApp.Features.Shared.Notifications;
using TodoApp.Features.Shared.Time;
using TodoApp.Features.Tasks.CreateTask;
using TodoApp.Features.Tasks.Recurrence;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.CreateTask;

// constructor injection is not supported. [Inject] is used here in lieu of the
// primary-constructor pattern applied to non-component classes.
public partial class CreateTaskForm
{
    [Inject] private CreateTaskHandler Handler { get; set; } = null!;
    [Inject] private CreateRecurringTaskHandler RecurringHandler { get; set; } = null!;
    [Inject] private NotificationService Notification { get; set; } = null!;
    [Inject] private ILogger<CreateTaskForm> Logger { get; set; } = null!;
    [Inject] private IUserClock Clock { get; set; } = null!;

    [Parameter] public EventCallback OnCreated { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private TaskFormModel Model { get; set; } = new();
    private RecurrenceFormModel Recurrence { get; } = new();

    private bool IsSubmitting { get; set; }
    private string? ErrorMessage { get; set; }

    private async Task SubmitAsync()
    {
        if (IsSubmitting) return;

        IsSubmitting = true;
        ErrorMessage = null;

        try
        {
            var created = Recurrence.Repeat == RepeatOption.None
                ? await CreateSingleTaskAsync()
                : await CreateRecurringTaskAsync();

            if (created)
            {
                await OnCreated.InvokeAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.UnexpectedError(OperationsTask.CreateTask, ex);
            Notification.Error("Une erreur inattendue est survenue. Réessayez plus tard.", "Erreur");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private async Task<bool> CreateSingleTaskAsync()
    {
        var result = await Handler.HandleAsync(
            new CreateTaskCommand(Model.Title, Model.Description, Model.Priority, Model.DueDate, Model.Type));

        if (result.IsFailure)
        {
            HandleFailure(result.Error, OperationsTask.CreateTask, "Création impossible");
            return false;
        }

        Notification.Success($"« {result.Value.Title} » a été créée.", "Tâche créée");
        return true;
    }

    private async Task<bool> CreateRecurringTaskAsync()
    {
        // L'échéance saisie devient la date de début de la série ; à défaut, aujourd'hui (fuseau de l'utilisateur).
        var startDate = Model.DueDate ?? Clock.Today;

        var result = await RecurringHandler.HandleAsync(new CreateRecurringTaskCommand(
            Model.Title,
            Model.Description,
            Model.Priority,
            Recurrence.ToFrequency(),
            Recurrence.Interval,
            Recurrence.Days,
            Recurrence.DayOfMonth ?? startDate.Day,
            startDate,
            EndDate: null,
            Model.Type));

        if (result.IsFailure)
        {
            HandleFailure(result.Error, OperationsTask.CreateRecurringTask, "Récurrence invalide");
            return false;
        }

        Notification.Success($"« {Model.Title.Trim()} » se répétera automatiquement.", "Tâche récurrente créée");
        return true;
    }

    private void HandleFailure(Error error, string operation, string title)
    {
        if (error.Type == ErrorType.Validation)
        {
            // Saisie à corriger : message dans le formulaire + toast, sans log (cas normal, pas un incident).
            ErrorMessage = error.Message;
            Notification.Warning(error.Message, "Formulaire incomplet");
            return;
        }

        Logger.OperationFailed(operation, error.Code, error.Message);
        Notification.Error(error.Message, title);
    }
}
