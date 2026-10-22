using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Features.Shared;

namespace TodoApp.Infrastructure.Configurations;

internal sealed partial class RecurringTaskScheduler(
    IDbContextFactory<ApplicationDbContext> factory,
    TimeProvider timeProvider,
    TaskChangeNotifier notifier,
    ILogger<RecurringTaskScheduler> logger)
{
    private const int BatchSize = 100;

    /// <summary>Crée les occurrences dues « aujourd'hui » dans le fuseau de chaque série. Idempotent.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = timeProvider.GetUtcNow();

        var latestPossibleToday = DateOnly.FromDateTime(utcNow.UtcDateTime.AddHours(14));

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var candidates = await db.RecurringTasks
            .AsTracking()
            .Where(s => !s.IsStopped && s.NextOccurrence != null && s.NextOccurrence <= latestPossibleToday)
            .OrderBy(s => s.NextOccurrence)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var created = 0;

        foreach (var series in candidates)
        {
            var today = TodayIn(series.TimeZoneId, utcNow);

            if (series.CollectDueOccurrence(today) is not { } dueDate) continue;

            var exists = await db.Tasks
                .IgnoreQueryFilters([QueryFilters.SoftDelete])
                .AnyAsync(t => t.RecurringTaskId == series.Id && t.DueDate == dueDate, cancellationToken);

            if (!exists)
            {
                db.Tasks.Add(series.CreateOccurrence(dueDate));
                created++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        if (created > 0)
        {
            notifier.NotifyChanged();
            LogCreated(created);
        }

        return created;
    }

    private static DateOnly TodayIn(string timeZoneId, DateTimeOffset utcNow)
    {
        var timeZone = TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var found) ? found : TimeZoneInfo.Utc;
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, timeZone).DateTime);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created {Count} recurring task occurrence(s)")]
    private partial void LogCreated(int count);
}
