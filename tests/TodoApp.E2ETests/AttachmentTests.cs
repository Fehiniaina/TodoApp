using Microsoft.Playwright;
using TodoApp.E2ETests.Infrastructure;

namespace TodoApp.E2ETests;

public sealed class AttachmentTests(E2EAppFixture app) : E2ETest(app)
{
    private static readonly byte[] Pdf = "%PDF-1.7\n% fichier de test E2E"u8.ToArray();

    [Fact]
    public async Task User_uploads_downloads_and_deletes_an_attachment()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");
        var taskId = await App.CreateTaskAsync(alice, "Tâche avec pièce jointe");
        await SignInAsync(alice);
        await Page.GotoAsync($"/tasks/{taskId}");
        await WaitForInteractivityAsync();

        // Ajout : on alimente directement le champ fichier caché sous la zone de dépôt.
        await Page.Locator("#task-attachment-input").SetInputFilesAsync(new FilePayload
        {
            Name = "Devis final.pdf",
            MimeType = "application/pdf",
            Buffer = Pdf,
        });

        var link = Page.GetByRole(AriaRole.Link, new() { Name = "Devis final.pdf" });
        await Expect(link).ToBeVisibleAsync();

        // Téléchargement : nom d'origine et contenu identique, octet pour octet.
        var download = await Page.RunAndWaitForDownloadAsync(() => link.ClickAsync());
        Assert.Equal("Devis final.pdf", download.SuggestedFilename);
        Assert.Equal(Pdf, await File.ReadAllBytesAsync(await download.PathAsync()));

        // Historique mis à jour sans rechargement.
        await Page.GetByRole(AriaRole.Tab, new() { Name = "Historique" }).ClickAsync();
        await Expect(Page.GetByText("a ajouté la pièce jointe « Devis final.pdf »")).ToBeVisibleAsync();

        // Suppression avec confirmation.
        await Page.GetByRole(AriaRole.Button, new() { Name = "Supprimer Devis final.pdf" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Oui" }).ClickAsync();

        await Expect(link).ToHaveCountAsync(0);
        await Expect(Page.GetByText("Aucune pièce jointe.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Executable_disguised_as_pdf_is_rejected_with_a_clear_message()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");
        var taskId = await App.CreateTaskAsync(alice, "Tâche");
        await SignInAsync(alice);
        await Page.GotoAsync($"/tasks/{taskId}");
        await WaitForInteractivityAsync();

        await Page.Locator("#task-attachment-input").SetInputFilesAsync(new FilePayload
        {
            Name = "facture.pdf",
            MimeType = "application/pdf",
            Buffer = [0x4D, 0x5A, 0x90, 0x00], // en-tête d'un exécutable Windows
        });

        await Expect(Page.GetByText("Le contenu du fichier ne correspond pas à son extension.")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Aucune pièce jointe.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Attach_button_opens_the_file_picker()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");
        var taskId = await App.CreateTaskAsync(alice, "Tâche");
        await SignInAsync(alice);
        await Page.GotoAsync($"/tasks/{taskId}");
        await WaitForInteractivityAsync();

        // Vérifie le lien label for → input : sans lui, le bouton « Joindre » ne ferait rien.
        var chooser = await Page.RunAndWaitForFileChooserAsync(() =>
            Page.GetByText("Joindre", new() { Exact = true }).ClickAsync());

        Assert.True(chooser.IsMultiple);
    }
}
