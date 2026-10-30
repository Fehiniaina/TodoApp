using TodoApp.Common;

namespace TodoApp.Features.Shared.Errors;

internal static class TaskErrors
{
    public static Error TitleRequired => Error.Validation("Task.TitleRequired", "A title is required.");

    public static Error TitleTooLong => Error.Validation("Task.TitleTooLong", "The title must be 200 characters or fewer.");

    public static Error DescriptionTooLong => Error.Validation("Task.DescriptionTooLong", "The description must be 2000 characters or fewer.");

    public static Error InvalidPriority => Error.Validation("Task.InvalidPriority", "The priority is invalid.");
    public static Error InvalidTaskType => Error.Validation("Task.InvalidTaskType", "Value of task type is invalid.");
}
