using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using TodoApp.Common;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared.Logging;
using TodoApp.Features.Shared.Notifications;
using TodoApp.Features.Tasks.CreateTask;
using TodoApp.Features.Tasks.Shared;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace TodoApp.Features.Tasks.CreateTask;

// constructor injection is not supported. [Inject] is used here in lieu of the
// primary-constructor pattern applied to non-component classes.
public partial class CreateTaskForm
{
    [Inject] private CreateTaskHandler Handler { get; set; } = null!;
    [Inject] private NotificationService Notification { get; set; } = null!;
    [Inject] private ILogger<CreateTaskForm> Logger { get; set; } = null!;

    [Parameter] public EventCallback OnCreated { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    // Cascading auth state is provided by AddCascadingAuthenticationState() in Program.cs.

    private TaskFormModel Model { get; set; } = new();
    private bool IsSubmitting { get; set; }
    private string? ErrorMessage { get; set; }
    private string? SuccessMessage { get; set; }

    private async Task SubmitAsync()
    {
        if (IsSubmitting) return;

        IsSubmitting = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var result = await Handler.HandleAsync(
                new CreateTaskCommand(Model.Title, string.IsNullOrWhiteSpace(Model.Description) ? null : Model.Description, Model.Priority, Model.DueDate));

            if (result.IsFailure)
            {
                if (result.Error.Type == ErrorType.Validation)
                {
                    ErrorMessage = result.Error.Message;
                    Notification.Warning(result.Error.Message, "Formulaire incomplet");
                }
                else
                {
                    Logger.OperationFailed(OperationsTask.CreateTask, result.Error.Code, result.Error.Message);
                    Notification.Error(result.Error.Message, "Création impossible");
                }

                return;
            }

            Notification.Success($"« {result.Value.Title} » a été créée.", "Tâche créée");
            await OnCreated.InvokeAsync();
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
}
