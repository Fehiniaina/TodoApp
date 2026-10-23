namespace TodoApp.Infrastructure.Email;

internal sealed record EmailMessage(
    string ToAddress,
    string ToName,
    string Subject,
    string HtmlBody,
    string TextBody,
    string? MessageId = null);
