using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace TodoApp.Infrastructure.Email;

internal sealed class MailKitEmailTransport(IOptions<SmtpOptions> options) : IEmailTransport
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var smtp = options.Value;

        using var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
        }.ToMessageBody();

        if (message.MessageId is not null)
        {
            mime.MessageId = message.MessageId;
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, smtp.Security, cancellationToken);

        if (smtp is { Username: { Length: > 0 } userName, Password: { } password })
        {
            await client.AuthenticateAsync(userName, password, cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
