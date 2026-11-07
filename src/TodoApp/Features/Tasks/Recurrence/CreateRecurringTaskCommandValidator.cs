using FluentValidation;
using TodoApp.Domain.Tasks.Recurrence;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Tasks.Recurrence;

internal sealed class CreateRecurringTaskCommandValidator : AbstractValidator<CreateRecurringTaskCommand>
{
    public CreateRecurringTaskCommandValidator()
    {
        RuleFor(c => c.Title).ValidTitle();
        RuleFor(c => c.Description).ValidDescription();
        RuleFor(c => c.Priority).ValidPriority();

        RuleFor(c => c.Frequency).IsInEnum();
        RuleFor(c => c.Interval).InclusiveBetween(1, 365)
            .WithMessage("L'intervalle doit être compris entre 1 et 365.");

        RuleFor(c => c.DaysOfWeek)
            .NotEqual(WeekDays.None)
            .When(c => c.Frequency == RecurrenceFrequency.Weekly)
            .WithMessage("Choisissez au moins un jour de la semaine.");

        RuleFor(c => c.DayOfMonth)
            .NotNull().InclusiveBetween(1, 31)
            .When(c => c.Frequency == RecurrenceFrequency.Monthly)
            .WithMessage("Le jour du mois doit être compris entre 1 et 31.");

        RuleFor(c => c.EndDate)
            .GreaterThanOrEqualTo(c => c.StartDate)
            .When(c => c.EndDate is not null)
            .WithMessage("La date de fin doit être postérieure à la date de début.");
    }
}
