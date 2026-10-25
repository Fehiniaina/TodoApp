using TodoApp.Infrastructure.Email.Templating;

namespace TodoApp.Features.Notifications.Email.Assignment;

public sealed record TaskAssignedEmail(
    string RecipientName,
    string ActorName,
    string TaskTitle,
    Uri TaskUrl) : IEmailModel
{
    public string Subject => $"{ActorName} vous a assigné « {TaskTitle} »";

    public string PlainText => $"""
        Bonjour {RecipientName},

        {ActorName} vous a assigné une tâche : {TaskTitle}

        Ouvrir la tâche : {TaskUrl.AbsoluteUri}
        """;
}
