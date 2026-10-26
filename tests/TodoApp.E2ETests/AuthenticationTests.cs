using System.Text.RegularExpressions;
using TodoApp.E2ETests.Infrastructure;

namespace TodoApp.E2ETests;

public sealed class AuthenticationTests(E2EAppFixture app) : E2ETest(app)
{
    [Fact]
    public async Task Anonymous_visitor_is_redirected_to_login()
    {
        await Page.GotoAsync("/tasks");

        await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login", RegexOptions.None, TimeSpan.FromSeconds(1)));
        await Expect(Page.GetByRole(Microsoft.Playwright.AriaRole.Heading, new() { Name = "Bon retour" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task User_signs_in_and_lands_on_the_requested_page()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");

        await Page.GotoAsync("/tasks");
        await Page.GetByLabel("Adresse e-mail").FillAsync(alice.Email);
        await Page.GetByLabel("Mot de passe", new() { Exact = true }).FillAsync(alice.Password);
        await Page.GetByRole(Microsoft.Playwright.AriaRole.Button, new() { Name = "Se connecter", Exact = true }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/tasks$", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task Protected_downloads_require_authentication()
    {
        // Contexte anonyme : la requête directe est redirigée vers la connexion, aucun fichier n'est servi.
        var response = await Page.APIRequest.GetAsync("/tasks/import/template", new() { MaxRedirects = 0 });

        Assert.Equal(302, response.Status);
        Assert.Contains("/Account/Login", response.Headers["location"], StringComparison.Ordinal);
    }
}
