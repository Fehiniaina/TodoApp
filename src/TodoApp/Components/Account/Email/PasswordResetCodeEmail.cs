using TodoApp.Infrastructure.Email.Templating;

namespace TodoApp.Components.Account.Email;

public sealed record PasswordResetCodeEmail(string RecipientName, string ResetCode) : IEmailModel
{
    public string Subject => "Votre code de réinitialisation";

    public string PlainText => $"""
        Bonjour {RecipientName},

        Voici votre code de réinitialisation de mot de passe :

        {ResetCode}

        Ce code est valable 24 heures et ne peut être utilisé qu'une fois.

        Si vous n'êtes pas à l'origine de cette demande, ignorez cet email : votre mot de passe reste inchangé.
        """;
}
