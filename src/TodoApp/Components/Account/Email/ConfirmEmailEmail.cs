using TodoApp.Infrastructure.Email.Templating;

namespace TodoApp.Components.Account.Email;

public sealed record ConfirmEmailEmail(string RecipientName, Uri ConfirmationUrl) : IEmailModel
{
    public string Subject => "Confirmez votre adresse email";

    public string PlainText => $"""
        Bonjour {RecipientName},

        Bienvenue sur TodoApp ! Pour activer votre compte, confirmez votre adresse email en ouvrant ce lien :

        {ConfirmationUrl.AbsoluteUri}

        Ce lien est valable 24 heures.

        Si vous n'avez pas créé de compte sur TodoApp, ignorez cet email.
        """;
}
