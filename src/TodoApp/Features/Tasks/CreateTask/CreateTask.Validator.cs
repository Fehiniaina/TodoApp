using FluentValidation;
using TodoApp.Features.Shared.Errors;

namespace TodoApp.Features.Tasks.CreateTask;

internal sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(c => c.OwnerId)
            .NotEmpty();

        RuleFor(c => c.Title)
            .NotEmpty()
                .WithErrorCode(TaskErrors.TitleRequired.Code)
                .WithMessage(TaskErrors.TitleRequired.Message)
            .MaximumLength(200)
                .WithErrorCode(TaskErrors.TitleTooLong.Code)
                .WithMessage(TaskErrors.TitleTooLong.Message);

        RuleFor(c => c.Description)
            .MaximumLength(2000)
                .WithErrorCode(TaskErrors.DescriptionTooLong.Code)
                .WithMessage(TaskErrors.DescriptionTooLong.Message)
            .When(c => c.Description is not null);

        RuleFor(c => c.Priority)
            .IsInEnum()
                .WithErrorCode(TaskErrors.InvalidPriority.Code)
                .WithMessage(TaskErrors.InvalidPriority.Message);
    }
}
