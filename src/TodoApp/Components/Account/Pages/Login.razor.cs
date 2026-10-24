using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using TodoApp.Data;

namespace TodoApp.Components.Account.Pages;

public partial class Login
{
    private string? errorMessage;
    private EditContext editContext = default!;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }
    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IdentityRedirectManager RedirectManager { get; set; } = null!;
    [Inject] private ILogger<Login> Logger { get; set; } = null!;

    private string RegisterUrl => NavigationManager.GetUriWithQueryParameters(
        "Account/Register",
        new Dictionary<string, object?>(StringComparer.Ordinal) { ["ReturnUrl"] = ReturnUrl });

    private const string ResendConfirmationUrl = "Account/ResendEmailConfirmation";

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        editContext = new EditContext(Input);

        if (HttpMethods.IsGet(HttpContext.Request.Method))
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    public async Task LoginUser()
    {
        if (!editContext.Validate())
        {
            return;
        }

        var result = await SignInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            LogUserLoggedIn(Logger, ReturnUrl);
            RedirectManager.RedirectTo(ReturnUrl);
        }
        else if (result.RequiresTwoFactor)
        {
            RedirectManager.RedirectTo(
                "Account/LoginWith2fa",
                new(StringComparer.Ordinal) { ["returnUrl"] = ReturnUrl, ["rememberMe"] = Input.RememberMe });
        }
        else if (result.IsLockedOut)
        {
            LogUserLockedOut(Logger);
            RedirectManager.RedirectTo("Account/Lockout");
        }
        else if (result.IsNotAllowed)
        {
            errorMessage = "Votre adresse e-mail n'est pas encore confirmée. Consultez le lien de confirmation.";
        }
        else
        {
            errorMessage = "E-mail ou mot de passe incorrect.";
        }
    }

    private sealed class InputModel
    {
        [Required(ErrorMessage = "L'adresse e-mail est requise.")]
        [EmailAddress(ErrorMessage = "Cette adresse e-mail n'est pas valide.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Le mot de passe est requis.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Rester connecté")]
        public bool RememberMe { get; set; }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Utilisateur connecté, redirection vers {ReturnUrl}")]
    private static partial void LogUserLoggedIn(ILogger logger, string? returnUrl);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Compte utilisateur verrouillé")]
    private static partial void LogUserLockedOut(ILogger logger);
}
