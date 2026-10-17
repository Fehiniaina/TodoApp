using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.CreateTask;

namespace TodoApp.Features.Tasks.CreateTask;

// Blazor components are instantiated by the framework with property injection;
// constructor injection is not supported. [Inject] is used here in lieu of the
// primary-constructor pattern applied to non-component classes.
public partial class CreateTaskForm
{
    [Inject] private CreateTaskHandler Handler { get; set; } = null!;

    // Cascading auth state is provided by AddCascadingAuthenticationState() in Program.cs.

    private FormModel Model { get; set; } = new();
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
                new CreateTaskCommand(Model.Title, string.IsNullOrWhiteSpace(Model.Description) ? null : Model.Description, Model.Priority));

            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }

            SuccessMessage = $"\"{result.Value.Title}\" created successfully.";
            Model = new FormModel();
        }
        finally
        {
            IsSubmitting = false;
        }   
    }

    internal sealed class FormModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    }
}
