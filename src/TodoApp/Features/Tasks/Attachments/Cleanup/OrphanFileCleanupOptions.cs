using System.ComponentModel.DataAnnotations;

namespace TodoApp.Features.Tasks.Attachments.Cleanup;

internal sealed class OrphanFileCleanupOptions
{
    public const string SectionName = "OrphanFileCleanup";

    [Range(typeof(TimeSpan), "00:01:00", "7.00:00:00")]
    public TimeSpan Interval { get; init; } = TimeSpan.FromDays(1);

    /// <summary>Âge minimal d'un fichier avant de pouvoir être supprimé.</summary>
    [Range(typeof(TimeSpan), "00:10:00", "7.00:00:00")]
    public TimeSpan GracePeriod { get; init; } = TimeSpan.FromHours(1);
}
