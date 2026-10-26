using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace TodoApp.E2ETests.Infrastructure;

[Collection(E2EGroup.Name)]
public abstract class E2ETest(E2EAppFixture app) : PageTest
{
    private readonly List<string> _browserLog = [];

    protected E2EAppFixture App { get; } = app;

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = App.BaseUrl.ToString(),
        Locale = "fr-FR",
        TimezoneId = "Indian/Mauritius",
        AcceptDownloads = true,
    };

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await Context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });

        Page.Console += (_, message) => Log($"console.{message.Type}", message.Text);
        Page.PageError += (_, error) => Log("exception JS", error);
        Page.RequestFailed += (_, request) => Log("requête échouée", $"{request.Url} → {request.Failure}");
        Page.Response += (_, response) =>
        {
            if (response.Status >= 400)
            {
                Log($"HTTP {response.Status}", response.Url);
            }
        };
        Page.WebSocket += (_, socket) =>
        {
            Log("websocket ouvert", socket.Url);
            socket.Close += (_, _) => Log("websocket fermé", socket.Url);
            socket.SocketError += (_, error) => Log("websocket erreur", error);
        };

        await App.ResetAsync();
    }

    protected async Task SignInAsync(TestUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        await Page.GotoAsync("/Account/Login");
        await Page.GetByLabel("Adresse e-mail").FillAsync(user.Email);
        await Page.GetByLabel("Mot de passe", new() { Exact = true }).FillAsync(user.Password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Se connecter", Exact = true }).ClickAsync();

        await Page.WaitForURLAsync(url => !url.Contains("/Account/Login", StringComparison.Ordinal));
    }

    /// <summary>Attend que le circuit Blazor soit connecté : avant, les clics sont perdus.</summary>
    protected async Task WaitForInteractivityAsync()
    {
        try
        {
            await Page.Locator("[data-interactive='true']").WaitForAsync(new() { Timeout = 15_000 });
        }
        catch (TimeoutException ex)
        {
            var markers = Page.Locator("[data-interactive]");
            var state = await markers.CountAsync() == 0
                ? "aucun élément data-interactive sur la page"
                : $"data-interactive = \"{await markers.First.GetAttributeAsync("data-interactive")}\"";

            var tracePath = Path.GetFullPath(Path.Combine(
                "playwright-traces",
                $"{GetType().Name}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip"));
            await Context.Tracing.StopAsync(new() { Path = tracePath });

            throw new TimeoutException(
                $"""
                Le circuit Blazor ne s'est pas connecté.
                Page : {Page.Url}
                État : {state}
                Trace : {tracePath}
                Journal du navigateur :
                {string.Join(Environment.NewLine, _browserLog)}
                """,
                ex);
        }
    }

    private void Log(string kind, string text) =>
        _browserLog.Add($"[{kind}] ({Page.Url}) {text}");
}
