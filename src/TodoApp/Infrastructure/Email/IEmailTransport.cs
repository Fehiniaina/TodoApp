namespace TodoApp.Infrastructure.Email;

internal interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
