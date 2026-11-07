using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TodoApp.IntegrationTests.Infrastructure;

/// <summary>
/// Met en pause la requête marquée par <c>TagWith(tag)</c> juste APRÈS son exécution :
/// la base a déjà répondu (instantané figé), mais le code appelant n'a pas encore repris la main.
/// Permet de provoquer une course de façon déterministe.
/// </summary>
internal sealed class PauseAfterQuery(string tag) : DbCommandInterceptor
{
    private readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Terminée quand la requête a été exécutée et attend.</summary>
    public Task Reached => _reached.Task;

    public void Release() => _released.TrySetResult();

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains($"-- {tag}", StringComparison.Ordinal))
        {
            _reached.TrySetResult();
            await _released.Task.WaitAsync(cancellationToken);
        }

        return result;
    }
}
