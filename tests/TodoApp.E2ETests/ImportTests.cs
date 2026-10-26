using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using TodoApp.E2ETests.Infrastructure;

namespace TodoApp.E2ETests;

public sealed class ImportTests(E2EAppFixture app) : E2ETest(app)
{
    [Fact]
    public async Task User_previews_then_imports_a_csv_file()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");
        await SignInAsync(alice);
        await Page.GotoAsync("/tasks/import");
        await WaitForInteractivityAsync();

        await Page.Locator("input[type=file]").SetInputFilesAsync(new FilePayload
        {
            Name = "taches.csv",
            MimeType = "text/csv",
            Buffer = Utf8WithBom(
                "Titre;Priorité;Échéance\n" +
                "Première tâche importée;Haute;15/11/2026\n" +
                "Seconde tâche importée;Basse;\n"),
        });

        await Expect(Page.GetByText("Prêt à importer")).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Importer 2 tâche(s)" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/tasks$", RegexOptions.None, TimeSpan.FromSeconds(1)));
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Première tâche importée" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Seconde tâche importée" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Invalid_row_blocks_the_import()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");
        await SignInAsync(alice);
        await Page.GotoAsync("/tasks/import");
        await WaitForInteractivityAsync();

        await Page.Locator("input[type=file]").SetInputFilesAsync(new FilePayload
        {
            Name = "taches.csv",
            MimeType = "text/csv",
            Buffer = Utf8WithBom("Titre;Priorité\nValide;Haute\nInvalide;Hte\n"),
        });

        await Expect(Page.GetByText("Priorité « Hte » inconnue", new() { Exact = false })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Importer 2 tâche(s)" })).ToBeDisabledAsync();
    }

    [Fact]
    public async Task Template_is_downloaded_as_a_csv_file()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");
        await SignInAsync(alice);
        await Page.GotoAsync("/tasks/import");

        var download = await Page.RunAndWaitForDownloadAsync(() =>
            Page.GetByRole(AriaRole.Link, new() { Name = "Télécharger le modèle" }).ClickAsync());

        Assert.Equal("modele-import-taches.csv", download.SuggestedFilename);
    }

    private static byte[] Utf8WithBom(string text)
    {
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return [.. encoding.GetPreamble(), .. encoding.GetBytes(text)];
    }
}
