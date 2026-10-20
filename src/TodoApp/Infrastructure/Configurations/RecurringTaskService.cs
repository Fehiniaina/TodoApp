using Microsoft.Extensions.Options;

namespace TodoApp.Infrastructure.Configurations;

internal sealed partial class RecurringTaskService(
    RecurringTaskScheduler scheduler,
    TimeProvider provider,
    IOptions<RecurringTaskOptions> options,
    ILogger<RecurringTaskService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval, provider);

        do
        {
            try
            {
                await scheduler.RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogFailed(ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Recurring task generation failed")]
    private partial void LogFailed(Exception ex);
}
