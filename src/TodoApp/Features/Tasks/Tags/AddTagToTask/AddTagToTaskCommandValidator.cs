using FluentValidation;
using TodoApp.Domain.Tags;

namespace TodoApp.Features.Tasks.Tags.AddTagToTask;

internal sealed class AddTagToTaskCommandValidator : AbstractValidator<AddTagToTaskCommand>
{
#pragma warning disable VSSpell001 // Spell Check
    public AddTagToTaskCommandValidator()
#pragma warning restore VSSpell001 // Spell Check
    {
        RuleFor(c => c.TagName)
            .Must(Tag.IsValidName)
            .WithErrorCode("Tag.InvalidName")
            .WithMessage($"Le nom d'un tag doit contenir entre 1 et {Tag.MaxNameLength} caractères.");
    }
}
