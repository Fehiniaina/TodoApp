using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Recurrence;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Data;

internal static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseTodoAppNpgsql(
        this DbContextOptionsBuilder options,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null) =>
        options
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MapEnum<TaskPriority>("task_priority");
                npgsql.MapEnum<RecurrenceFrequency>("recurrence_frequency");
                configureNpgsql?.Invoke(npgsql);
            })
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(DomainEventsInterceptor.Instance);
}
