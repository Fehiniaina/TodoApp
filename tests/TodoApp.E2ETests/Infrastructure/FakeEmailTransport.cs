using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using TodoApp.Infrastructure.Email;

namespace TodoApp.E2ETests.Infrastructure;

internal sealed class FakeEmailTransport : IEmailTransport
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyCollection<EmailMessage> Sent => _sent;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public void Clear() => _sent.Clear();
}
