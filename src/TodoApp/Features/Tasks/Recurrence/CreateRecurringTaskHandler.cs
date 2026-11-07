using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks.Recurrence;
using TodoApp.Features.Shared;
using TodoApp.Features.Shared.Time;

namespace TodoApp.Features.Tasks.Recurrence;

internal sealed class CreateRecurringTaskHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    IValidator<CreateRecurringTaskCommand> validator,
    ICurrentUser currentUser,
    UserTimeZone userTimeZone,
    IUserClock clock,
    TimeProvider timeProvider,
    TaskChangeNotifier notifier) : IHandler
{
    public async Task<Result<Guid>> HandleAsync(CreateRecurringTaskCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation("RecurringTask.IsInvalid", validation.Errors[0].ErrorMessage);
        }

        var rule = command.Frequency switch
        {
            RecurrenceFrequency.Daily => RecurrenceRule.Daily(command.Interval),
            RecurrenceFrequency.Weekly => RecurrenceRule.Weekly(command.DaysOfWeek, command.Interval),
            _ => RecurrenceRule.Monthly(command.DayOfMonth!.Value, command.Interval),
        };

        var series = RecurringTask.Create(
            await currentUser.GetIdAsync(),
            command.Title, command.Description, command.Priority,
            rule, command.StartDate, command.EndDate,
            userTimeZone.TimeZone.Id,
            timeProvider.GetUtcNow().UtcDateTime,
            command.Type);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.RecurringTasks.Add(series);

        if (series.CollectDueOccurrence(clock.Today) is { } dueDate)
        {
            db.Tasks.Add(series.CreateOccurrence(dueDate));
        }

        await db.SaveChangesAsync(cancellationToken);
        notifier.NotifyChanged();

        return series.Id;
    }
}
