using System.ComponentModel.DataAnnotations;
using MailKit.Security;

namespace TodoApp.Infrastructure.Email;

internal sealed class SmtpOptions : IValidatableObject
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    public SecureSocketOptions Security { get; init; } = SecureSocketOptions.StartTls;

    public string? Username { get; init; }

    public string? Password { get; init; }

    [Required, EmailAddress]
    public string FromAddress { get; init; } = string.Empty;

    [Required]
    public string FromName { get; init; } = "TodoApp";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(Username) && string.IsNullOrEmpty(Password))
        {
            yield return new ValidationResult(
                "Smtp:Password est requis lorsque Smtp:Username est renseigné.",
                [nameof(Password)]);
        }
    }
}
