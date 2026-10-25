using FluentValidation;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Tasks.Import;

internal sealed class ImportedTaskFieldsValidator : AbstractValidator<ImportedTaskFields>
{
    public ImportedTaskFieldsValidator()
    {
        RuleFor(f => f.Title).ValidTitle();
        RuleFor(f => f.Description).ValidDescription();
    }
}
