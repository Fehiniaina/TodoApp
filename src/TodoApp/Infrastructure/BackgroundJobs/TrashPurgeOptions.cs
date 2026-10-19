using System.ComponentModel.DataAnnotations;

namespace TodoApp.Infrastructure.BackgroundJobs;

internal sealed class TrashPurgeOptions
{
    public const string SectionName = "TrashPurge";

    [Range(typeof(TimeSpan), "00:01:00", "365.00:00:00",
        ErrorMessage = "TrashPurge:Retention doit être comprise entre 1 minute et 365 jours.")]
    public TimeSpan Retention { get; set; } = TimeSpan.FromMinutes(4);

    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00",
        ErrorMessage = "TrashPurge:Interval doit être compris entre 1 minute et 24 heures.")]
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(3);
}
