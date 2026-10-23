using System.ComponentModel.DataAnnotations;

namespace TodoApp.Features.Notifications.Email;

internal sealed class EmailDeliveryOptions
{
    public const string SectionName = "EmailDelivery";

    [Range(typeof(TimeSpan), "00:00:05", "01:00:00")]
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(30);

    [Range(1, 200)]
    public int BatchSize { get; init; } = 20;

    [Range(1, 20)]
    public int MaxAttempts { get; init; } = 6;

    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromHours(1);

    [Range(typeof(TimeSpan), "00:10:00", "7.00:00:00")]
    public TimeSpan ExpireAfter { get; init; } = TimeSpan.FromDays(1);

    [Required]
    public Uri AppBaseUrl { get; init; } = null!;
}
