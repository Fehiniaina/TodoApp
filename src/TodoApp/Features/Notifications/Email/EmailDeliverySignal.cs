using System.Threading.Channels;

namespace TodoApp.Features.Notifications.Email;

/// <summary>Réveille le job d'envoi dès qu'un email est en attente, sans attendre le prochain cycle.</summary>
internal sealed class EmailDeliverySignal
{
    // Capacité 1 : dix notifications rapprochées ne déclenchent qu'un seul réveil.
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void Notify() => _channel.Writer.TryWrite(true);

    /// <summary>Attend un signal ou la fin du délai, au premier des deux.</summary>
    public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            await _channel.Reader.ReadAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Délai écoulé sans signal : cycle normal.
        }
    }
}
