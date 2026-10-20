using FluentValidation;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared.Errors;

namespace TodoApp.Features.Shared;

/// <summary>Règles communes aux commandes de création / modification de tâche.</summary>
internal static class TaskRules
{
    public static IRuleBuilderOptions<T, string> ValidTitle<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
                .WithErrorCode(TaskErrors.TitleRequired.Code)
                .WithMessage(TaskErrors.TitleRequired.Message)
            .MaximumLength(200)
                .WithErrorCode(TaskErrors.TitleTooLong.Code)
                .WithMessage(TaskErrors.TitleTooLong.Message);

    public static IRuleBuilderOptions<T, string?> ValidDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(2000)
            .WithErrorCode(TaskErrors.DescriptionTooLong.Code)
            .WithMessage(TaskErrors.DescriptionTooLong.Message);

    public static IRuleBuilderOptions<T, TaskPriority> ValidPriority<T>(this IRuleBuilder<T, TaskPriority> rule) =>
        rule.IsInEnum()
            .WithErrorCode(TaskErrors.InvalidPriority.Code)
            .WithMessage(TaskErrors.InvalidPriority.Message);
}
