using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TodoApp.IntegrationTests.Infrastructure;

/// <summary>Capture le SQL et les paramètres des requêtes exécutées (diagnostic).</summary>
internal sealed class CommandCapture : DbCommandInterceptor
{
    public List<(string Sql, (string Name, object? Value)[] Parameters)> Commands { get; } = [];

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Commands.Add((
            command.CommandText,
            [.. command.Parameters.Cast<DbParameter>().Select(p => (p.ParameterName, p.Value))]));

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
