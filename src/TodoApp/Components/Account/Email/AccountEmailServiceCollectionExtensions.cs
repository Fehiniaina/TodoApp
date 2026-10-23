using Microsoft.AspNetCore.Identity;
using TodoApp.Data;
using TodoApp.Infrastructure.Email.Templating;

namespace TodoApp.Components.Account.Email;

internal static class AccountEmailServiceCollectionExtensions
{
    public static IServiceCollection AddAccountEmails(this IServiceCollection services)
    {
        services.AddEmailTemplate<ConfirmEmailEmail, ConfirmEmailEmailTemplate>();
        services.AddEmailTemplate<PasswordResetEmail, PasswordResetEmailTemplate>();
        services.AddEmailTemplate<PasswordResetCodeEmail, PasswordResetCodeEmailTemplate>();

        services.AddSingleton<IEmailSender<ApplicationUser>, IdentityEmailSender>();

        return services;
    }
}
