using TodoApp.Infrastructure.Email.Templating;

namespace TodoApp.Components.Account.Email;

public sealed record PasswordResetEmail(string RecipientName, Uri ResetUrl) : IEmailModel
{
    public string Subject => "Réinitialisation de votre mot de passe";

    public string PlainText => $"""
        Bonjour {RecipientName},

        Vous avez demandé à réinitialiser le mot de passe de votre compte TodoApp.
        Pour choisir un nouveau mot de passe, ouvrez ce lien :

        {ResetUrl.AbsoluteUri}

        Ce lien est valable 24 heures et ne peut être utilisé qu'une fois.

        Si vous n'êtes pas à l'origine de cette demande, ignorez cet email : votre mot de passe reste inchangé.
        """;
}
