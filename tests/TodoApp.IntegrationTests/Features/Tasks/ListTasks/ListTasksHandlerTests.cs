using TodoApp.Common;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.ListTasks;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Features.Tasks.ListTasks;

[Collection("Postgres")]
public sealed class ListTasksHandlerTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string OwnerA = "owner-A";
    private const string OwnerB = "owner-B";
    private const string OwnerC = "owner-C";

    private static readonly DateOnly Today = new(2026, 10, 15);
    private static readonly DateTime BaseCreatedAtUtc = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    // Chaque tâche est créée une minute après la précédente : tri par date de création déterministe.
    private int _seeded;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(OwnerA, OwnerB, OwnerC);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Without_filter_returns_only_tasks_visible_to_the_user()
    {
        await SeedAsync("A créée par A", owner: OwnerA);
        await SeedAsync("B assignée à A", owner: OwnerB, assigneeId: OwnerA);
        await SeedAsync("B privée", owner: OwnerB);

        var titles = await TitlesAsync(TaskFilter.None, userId: OwnerA);

        Assert.Equal(["A créée par A", "B assignée à A"], titles);
    }

    [Fact]
    public async Task Assignee_filter_never_reveals_tasks_the_user_cannot_see()
    {
        // B a une tâche privée assignée à C : filtrer sur C ne doit pas la montrer à A.
        await SeedAsync("B privée assignée à C", owner: OwnerB, assigneeId: OwnerC);
        await SeedAsync("A assignée à C", owner: OwnerA, assigneeId: OwnerC);

        var titles = await TitlesAsync(Assignees(OwnerC), userId: OwnerA);

        Assert.Equal(["A assignée à C"], titles);
    }

    [Fact]
    public async Task Scope_assigned_to_me_returns_only_my_assignments()
    {
        await SeedAsync("A pour moi", owner: OwnerA, assigneeId: OwnerA);
        await SeedAsync("A pour B", owner: OwnerA, assigneeId: OwnerB);
        await SeedAsync("B pour moi", owner: OwnerB, assigneeId: OwnerA);

        var titles = await TitlesAsync(TaskFilter.None, userId: OwnerA, scope: TaskScope.AssignedToMe);

        Assert.Equal(["A pour moi", "B pour moi"], titles);
    }

    [Fact]
    public async Task Scope_created_by_me_returns_only_my_tasks()
    {
        await SeedAsync("Créée par A", owner: OwnerA);
        await SeedAsync("Créée par B, assignée à A", owner: OwnerB, assigneeId: OwnerA);

        var titles = await TitlesAsync(TaskFilter.None, userId: OwnerA, scope: TaskScope.CreatedByMe);

        Assert.Equal(["Créée par A"], titles);
    }

    [Fact]
    public async Task Scope_and_filter_are_combined()
    {
        await SeedAsync("Mienne haute", owner: OwnerA, assigneeId: OwnerA, priority: TaskPriority.High);
        await SeedAsync("Mienne basse", owner: OwnerA, assigneeId: OwnerA, priority: TaskPriority.Low);
        await SeedAsync("À B haute", owner: OwnerA, assigneeId: OwnerB, priority: TaskPriority.High);

        var titles = await TitlesAsync(
            Priorities(TaskPriority.High), userId: OwnerA, scope: TaskScope.AssignedToMe);

        Assert.Equal(["Mienne haute"], titles);
    }

    [Fact]
    public async Task Priority_filter_returns_selected_priorities()
    {
        await SeedAsync("Basse", priority: TaskPriority.Low);
        await SeedAsync("Moyenne", priority: TaskPriority.Medium);
        await SeedAsync("Haute", priority: TaskPriority.High);
        await SeedAsync("Urgente", priority: TaskPriority.Urgent);

        var titles = await TitlesAsync(Priorities(TaskPriority.High, TaskPriority.Urgent));

        Assert.Equal(["Haute", "Urgente"], titles);
    }

    [Fact]
    public async Task Type_filter_returns_selected_types()
    {
        await SeedAsync("Un bug", type: TaskType.Bug);
        await SeedAsync("Une story", type: TaskType.Story);
        await SeedAsync("Une tâche", type: TaskType.Task);
        await SeedAsync("Une extraction", type: TaskType.Extraction);

        var titles = await TitlesAsync(Types(TaskType.Story, TaskType.Extraction));

        Assert.Equal(["Une extraction", "Une story"], titles);
    }

    [Fact]
    public async Task Type_filter_on_bug_works_despite_zero_value()
    {
        // Bug = 0 : rappel du piège de la sentinelle, la valeur zéro doit être persistée ET filtrée.
        await SeedAsync("Un bug", type: TaskType.Bug);
        await SeedAsync("Une tâche", type: TaskType.Task);

        var titles = await TitlesAsync(Types(TaskType.Bug));

        Assert.Equal(["Un bug"], titles);
    }

    [Fact]
    public async Task Assignee_filter_returns_tasks_assigned_to_selected_users()
    {
        await SeedAsync("Pour B", assigneeId: OwnerB);
        await SeedAsync("Pour C", assigneeId: OwnerC);
        await SeedAsync("Non assignée");

        var titles = await TitlesAsync(Assignees(OwnerB));

        Assert.Equal(["Pour B"], titles);
    }

    [Fact]
    public async Task Unassigned_filter_returns_only_unassigned_tasks()
    {
        await SeedAsync("Pour B", assigneeId: OwnerB);
        await SeedAsync("Non assignée");

        var titles = await TitlesAsync(new TaskFilter { IncludeUnassigned = true });

        Assert.Equal(["Non assignée"], titles);
    }

    [Fact]
    public async Task Users_and_unassigned_are_combined_with_or()
    {
        await SeedAsync("Pour B", assigneeId: OwnerB);
        await SeedAsync("Pour C", assigneeId: OwnerC);
        await SeedAsync("Non assignée");

        var titles = await TitlesAsync(Assignees(OwnerB) with { IncludeUnassigned = true });

        Assert.Equal(["Non assignée", "Pour B"], titles);
    }

    [Fact]
    public async Task Overdue_returns_open_tasks_with_past_due_date()
    {
        await SeedAsync("En retard", dueDate: Today.AddDays(-1));
        await SeedAsync("En retard en cours", dueDate: Today.AddDays(-5), status: TaskItemStatus.InProgress);
        await SeedAsync("Résolue en retard", dueDate: Today.AddDays(-1), status: TaskItemStatus.Resolved);
        await SeedAsync("Aujourd'hui", dueDate: Today);
        await SeedAsync("Sans échéance");

        var titles = await TitlesAsync(Due(DueDateFilter.Overdue));

        Assert.Equal(["En retard", "En retard en cours"], titles);
    }

    [Fact]
    public async Task Today_returns_tasks_due_today()
    {
        await SeedAsync("Hier", dueDate: Today.AddDays(-1));
        await SeedAsync("Aujourd'hui", dueDate: Today);
        await SeedAsync("Demain", dueDate: Today.AddDays(1));

        var titles = await TitlesAsync(Due(DueDateFilter.Today));

        Assert.Equal(["Aujourd'hui"], titles);
    }

    [Fact]
    public async Task Next7Days_returns_today_to_day_plus_six()
    {
        await SeedAsync("Hier", dueDate: Today.AddDays(-1));
        await SeedAsync("J0", dueDate: Today);
        await SeedAsync("J6", dueDate: Today.AddDays(6));
        await SeedAsync("J7", dueDate: Today.AddDays(7));
        await SeedAsync("Sans échéance");

        var titles = await TitlesAsync(Due(DueDateFilter.Next7Days));

        Assert.Equal(["J0", "J6"], titles);
    }

    [Fact]
    public async Task NoDueDate_returns_tasks_without_due_date()
    {
        await SeedAsync("Avec échéance", dueDate: Today);
        await SeedAsync("Sans échéance");

        var titles = await TitlesAsync(Due(DueDateFilter.NoDueDate));

        Assert.Equal(["Sans échéance"], titles);
    }

    [Fact]
    public async Task Range_is_inclusive_on_both_bounds()
    {
        await SeedAsync("Avant", dueDate: Today.AddDays(-1));
        await SeedAsync("Début", dueDate: Today);
        await SeedAsync("Fin", dueDate: Today.AddDays(5));
        await SeedAsync("Après", dueDate: Today.AddDays(6));
        await SeedAsync("Sans échéance");

        var titles = await TitlesAsync(
            new TaskFilter { DueDate = DueDateFilter.Range, DueFrom = Today, DueTo = Today.AddDays(5) });

        Assert.Equal(["Début", "Fin"], titles);
    }

    [Fact]
    public async Task Range_with_only_lower_bound_is_open_ended()
    {
        await SeedAsync("Avant", dueDate: Today.AddDays(-1));
        await SeedAsync("Bien après", dueDate: Today.AddYears(1));

        var titles = await TitlesAsync(new TaskFilter { DueDate = DueDateFilter.Range, DueFrom = Today });

        Assert.Equal(["Bien après"], titles);
    }

    [Fact]
    public async Task All_criteria_are_combined_with_and()
    {
        await SeedAsync("Tout correspond",
            priority: TaskPriority.High, type: TaskType.Bug, assigneeId: OwnerB, dueDate: Today.AddDays(-1));
        await SeedAsync("Mauvaise priorité",
            priority: TaskPriority.Low, type: TaskType.Bug, assigneeId: OwnerB, dueDate: Today.AddDays(-1));
        await SeedAsync("Mauvais type",
            priority: TaskPriority.High, type: TaskType.Story, assigneeId: OwnerB, dueDate: Today.AddDays(-1));
        await SeedAsync("Mauvais assigné",
            priority: TaskPriority.High, type: TaskType.Bug, assigneeId: OwnerC, dueDate: Today.AddDays(-1));
        await SeedAsync("Pas en retard",
            priority: TaskPriority.High, type: TaskType.Bug, assigneeId: OwnerB, dueDate: Today.AddDays(1));

        var filter = new TaskFilter
        {
            Priorities = new HashSet<TaskPriority> { TaskPriority.High },
            Types = new HashSet<TaskType> { TaskType.Bug },
            AssigneeIds = new HashSet<string>(StringComparer.Ordinal) { OwnerB },
            DueDate = DueDateFilter.Overdue,
        };

        var titles = await TitlesAsync(filter);

        Assert.Equal(["Tout correspond"], titles);
    }

    [Fact]
    public async Task Total_count_reflects_the_filter_not_the_page()
    {
        for (var i = 1; i <= 5; i++)
        {
            await SeedAsync($"Haute {i}", priority: TaskPriority.High);
        }
        await SeedAsync("Basse", priority: TaskPriority.Low);

        var result = await QueryAsync(Priorities(TaskPriority.High), page: 1, pageSize: 2);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Pages_cover_every_task_exactly_once_newest_first()
    {
        for (var i = 1; i <= 5; i++)
        {
            await SeedAsync($"Tâche {i}");
        }

        var page1 = await QueryAsync(TaskFilter.None, page: 1, pageSize: 2);
        var page2 = await QueryAsync(TaskFilter.None, page: 2, pageSize: 2);
        var page3 = await QueryAsync(TaskFilter.None, page: 3, pageSize: 2);

        var all = page1.Items.Concat(page2.Items).Concat(page3.Items).Select(t => t.Title).ToList();

        // Plus récente en premier ; aucune tâche ne saute ni ne se répète.
        Assert.Equal(["Tâche 5", "Tâche 4", "Tâche 3", "Tâche 2", "Tâche 1"], all);
    }

    [Fact]
    public async Task Page_size_is_clamped_to_maximum()
    {
        await SeedAsync("Unique");

        var result = await QueryAsync(TaskFilter.None, page: 1, pageSize: 10_000);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Summary_exposes_type_priority_and_assignee()
    {
        await SeedAsync("Détail", priority: TaskPriority.Urgent, type: TaskType.Story, assigneeId: OwnerB);

        var result = await QueryAsync(TaskFilter.None);

        var summary = Assert.Single(result.Items);
        Assert.Equal(TaskType.Story, summary.Type);
        Assert.Equal(TaskPriority.Urgent, summary.Priority);
        Assert.Equal(OwnerB, summary.AssigneeId);
    }

    private async Task<TaskItem> SeedAsync(
        string title,
        string owner = OwnerA,
        TaskPriority priority = TaskPriority.Medium,
        TaskType type = TaskType.Task,
        DateOnly? dueDate = null,
        string? assigneeId = null,
        TaskItemStatus status = TaskItemStatus.ToDo)
    {
        var createdAtUtc = BaseCreatedAtUtc.AddMinutes(++_seeded);
        var task = TaskItem.Create(owner, title, null, priority, dueDate, createdAtUtc, type);

        if (assigneeId is not null)
        {
            task.AssignTo(assigneeId, owner, createdAtUtc);
        }

        foreach (var step in PathTo(status))
        {
            task.ChangeStatus(step, owner, createdAtUtc);
        }

        await using var db = fixture.CreateDbContext();
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        return task;
    }

    private async Task<PagedResult<TaskSummary>> QueryAsync(
        TaskFilter filter,
        string userId = OwnerA,
        TaskScope scope = TaskScope.All,
        int page = 1,
        int pageSize = 100)
    {
        var handler = new ListTasksHandler(new TestDbContextFactory(fixture), new FakeCurrentUser(userId));

        var result = await handler.HandleAsync(
            new ListTasksQuery(scope, Page: page, PageSize: pageSize) { Filter = filter, Today = Today });

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value;
    }

    /// <summary>Titres triés : l'ordre de la liste est vérifié séparément, par les tests de pagination.</summary>
    private async Task<IReadOnlyList<string>> TitlesAsync(
        TaskFilter filter,
        string userId = OwnerA,
        TaskScope scope = TaskScope.All)
    {
        var result = await QueryAsync(filter, userId, scope);
        return [.. result.Items.Select(t => t.Title).Order(StringComparer.Ordinal)];
    }

    private static TaskFilter Priorities(params TaskPriority[] priorities) =>
        new() { Priorities = new HashSet<TaskPriority>(priorities) };

    private static TaskFilter Types(params TaskType[] types) =>
        new() { Types = new HashSet<TaskType>(types) };

    private static TaskFilter Assignees(params string[] ids) =>
        new() { AssigneeIds = new HashSet<string>(ids, StringComparer.Ordinal) };

    private static TaskFilter Due(DueDateFilter mode) => new() { DueDate = mode };

    /// <summary>Plus court chemin autorisé par le workflow depuis « À faire ».</summary>
    private static Stack<TaskItemStatus> PathTo(TaskItemStatus target)
    {
        var previous = new Dictionary<TaskItemStatus, TaskItemStatus>();
        var visited = new HashSet<TaskItemStatus> { TaskItemStatus.ToDo };
        var queue = new Queue<TaskItemStatus>();
        queue.Enqueue(TaskItemStatus.ToDo);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target)
            {
                break;
            }

            foreach (var next in TaskWorkflow.NextStatuses(current))
            {
                if (visited.Add(next))
                {
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
        }

        if (!visited.Contains(target))
        {
            throw new InvalidOperationException($"Statut {target} inaccessible depuis ToDo dans le workflow.");
        }

        var path = new Stack<TaskItemStatus>();
        for (var status = target; status != TaskItemStatus.ToDo; status = previous[status])
        {
            path.Push(status);
        }

        return path;
    }
}
