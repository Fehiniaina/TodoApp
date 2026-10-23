using Microsoft.AspNetCore.Identity;
using TodoApp.Components.Account.Email;
using TodoApp.Data;
using TodoApp.Features.Users;
using TodoApp.Infrastructure.Email;
using TodoApp.Infrastructure.Email.Templating;

namespace TodoApp.Components.Account;

internal sealed class IdentityEmailSender(IEmailRenderer renderer, IEmailTransport transport)
    : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, new ConfirmEmailEmail(DisplayName(user), new Uri(HtmlDecode(confirmationLink))));

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, new PasswordResetEmail(DisplayName(user), new Uri(HtmlDecode(resetLink))));

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, new PasswordResetCodeEmail(DisplayName(user), resetCode));

    private async Task SendAsync(string to, IEmailModel model)
    {
        var rendered = await renderer.RenderAsync(model);
        await transport.SendAsync(
            new EmailMessage(to, to, rendered.Subject, rendered.Html, rendered.Text),
            CancellationToken.None);
    }

    // Le template Identity encode déjà le lien ; Razor le réencoderait.
    private static string HtmlDecode(string value) => System.Net.WebUtility.HtmlDecode(value);

    private static string DisplayName(ApplicationUser user) =>
        UserSummary.ToDisplayName(user.FirstName, user.LastName, user.Email);
}
