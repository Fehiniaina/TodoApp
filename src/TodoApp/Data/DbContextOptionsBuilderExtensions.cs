using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using TodoApp.Domain.Tasks;

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
                configureNpgsql?.Invoke(npgsql);   // réglages propres à l'hôte (retry…)
            })
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .UseSnakeCaseNamingConvention();
}