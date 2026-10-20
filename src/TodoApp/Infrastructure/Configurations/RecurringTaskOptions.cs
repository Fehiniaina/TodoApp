using System.ComponentModel.DataAnnotations;

namespace TodoApp.Infrastructure.Configurations;

internal sealed class RecurringTaskOptions
{
    public const string SectionName = "RecurringTasks";

    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00",
        ErrorMessage = "RecurringTasks:Interval doit être compris entre 1 minute et 24 heures.")]
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);
}
