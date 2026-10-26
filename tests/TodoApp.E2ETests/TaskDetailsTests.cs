using System.Text.RegularExpressions;
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
}
