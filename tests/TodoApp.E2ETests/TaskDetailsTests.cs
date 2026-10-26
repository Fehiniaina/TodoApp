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
}
