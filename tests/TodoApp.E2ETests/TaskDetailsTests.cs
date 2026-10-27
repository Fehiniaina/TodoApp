using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright;
using TodoApp.E2ETests.Infrastructure;

namespace TodoApp.E2ETests;

public sealed class TaskDetailsTests(E2EAppFixture app) : E2ETest(app)
{
    [Fact]
    public async Task Clicking_a_task_title_opens_its_detail_page()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");
        var taskId = await App.CreateTaskAsync(alice, "Préparer la démo client", "Slides et jeu de données");
        await SignInAsync(alice);

        await Page.GotoAsync("/tasks");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Préparer la démo client" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex($"/tasks/{taskId}$", RegexOptions.None, TimeSpan.FromSeconds(1)));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Préparer la démo client", Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Slides et jeu de données")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Task_of_another_user_is_not_accessible()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");
        var carol = await App.CreateUserAsync("Carol", "Durand");
        var taskId = await App.CreateTaskAsync(alice, "Tâche privée");
        await SignInAsync(carol);

        await Page.GotoAsync($"/tasks/{taskId}");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Tâche introuvable" })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Tâche privée")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task User_renames_the_task_in_place()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");
        var taskId = await App.CreateTaskAsync(alice, "Titre initial");
        await SignInAsync(alice);
        await Page.GotoAsync($"/tasks/{taskId}");
        await WaitForInteractivityAsync();

        await Page.GetByRole(AriaRole.Heading, new() { Name = "Titre initial", Level = 1 }).ClickAsync();
        var input = Page.GetByLabel("Titre", new() { Exact = true });
        await input.FillAsync("Titre modifié");
        await input.PressAsync("Enter");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Titre modifié", Level = 1 })).ToBeVisibleAsync();

        // Persisté : visible après rechargement complet de la page.
        await Page.ReloadAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Titre modifié", Level = 1 })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Empty_title_is_refused_and_the_editor_stays_open()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");
        var taskId = await App.CreateTaskAsync(alice, "Titre initial");
        await SignInAsync(alice);
        await Page.GotoAsync($"/tasks/{taskId}");
        await WaitForInteractivityAsync();

        await Page.GetByRole(AriaRole.Heading, new() { Name = "Titre initial", Level = 1 }).ClickAsync();
        var input = Page.GetByLabel("Titre", new() { Exact = true });
        await input.FillAsync("   ");
        await input.PressAsync("Enter");

        await Expect(Page.GetByRole(AriaRole.Alert)).ToContainTextAsync("obligatoire");
        await Expect(input).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Status_offers_only_allowed_transitions_and_is_saved()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");
        var taskId = await App.CreateTaskAsync(alice, "Tâche à démarrer");
        await SignInAsync(alice);
        await Page.GotoAsync($"/tasks/{taskId}");
        await WaitForInteractivityAsync();

        // Depuis « À faire », seul « En cours » est proposé (TaskWorkflow).
        await Expect(Field("Statut").Locator("option")).ToHaveCountAsync(2);

        await SaveFieldAsync(() => Field("Statut").SelectOptionAsync(new SelectOptionValue { Label = "En cours" }));

        // Rendu par le serveur après l'enregistrement : couleur du badge et nouvelles transitions.
        await Expect(Field("Statut")).ToHaveClassAsync(new Regex("lozenge-progress", RegexOptions.None, TimeSpan.FromSeconds(1)));
        await Expect(Field("Statut").Locator("option")).ToHaveCountAsync(3); // En cours, En validation, À faire

        await Page.ReloadAsync();
        await WaitForInteractivityAsync();
        await Expect(Field("Statut")).ToHaveValueAsync("InProgress");
    }

    [Fact]
    public async Task User_assigns_the_task_to_himself_and_changes_priority_and_due_date()
    {
        var alice = await App.CreateUserAsync("Alice", "Martin");
        var taskId = await App.CreateTaskAsync(alice, "Tâche à organiser");
        await SignInAsync(alice);
        await Page.GotoAsync($"/tasks/{taskId}");
        await WaitForInteractivityAsync();

        await SaveFieldAsync(() => Page.GetByRole(AriaRole.Button, new() { Name = "M'assigner" }).ClickAsync());
        await SaveFieldAsync(() => Field("Priorité").SelectOptionAsync(new SelectOptionValue { Label = "Haute" }));
        await SaveFieldAsync(() => Field("Échéance").FillAsync("2026-12-15"));

        await Page.ReloadAsync();
        await WaitForInteractivityAsync();

        await Expect(Field("Assigné")).ToHaveValueAsync(alice.Id);
        await Expect(Field("Priorité")).ToHaveValueAsync("High");
        await Expect(Field("Échéance")).ToHaveValueAsync("2026-12-15");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "M'assigner" })).ToHaveCountAsync(0);
    }

    /// <summary>Fait une modification dans le panneau, puis attend que le serveur l'ait enregistrée et rechargée.</summary>
    private async Task SaveFieldAsync(Func<Task> change)
    {
        var sidebar = Page.Locator("[data-fields-version]");
        var before = await sidebar.GetAttributeAsync("data-fields-version");

        await change();

        await Expect(sidebar).Not.ToHaveAttributeAsync("data-fields-version", before!);
    }

    private ILocator Field(string label) => Page.GetByLabel(label, new() { Exact = true });
}
