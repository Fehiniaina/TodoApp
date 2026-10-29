using System.Globalization;
using Bogus;

using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Domain.Tasks;

namespace TodoApp.Infrastructure.Seed;

public static class TodoAppSeeder
{
    private const int TaskCount = 30;

    private static readonly TaskItemStatus[] WorkflowPath =
    [
        TaskItemStatus.InProgress,
        TaskItemStatus.InValidation,
        TaskItemStatus.Resolved,
    ];

    public static async Task SeedAsync(IDbContextFactory<ApplicationDbContext> factory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var userIds = await context.Users.Select(u => u.Id).ToListAsync(cancellationToken);
        if (userIds.Count == 0)
        {
            return;
        }

        if (await context.Tasks.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        Randomizer.Seed = new Random(20261021);

        var faker = new Faker("fr");
        var nowUtc = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(nowUtc);

        var tasks = faker.Random
            .Shuffle(SeedCatalog.Tasks)
            .Take(TaskCount)
            .Select(template => CreateTask(faker, template, userIds, today, nowUtc))
            .ToList();

        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync(cancellationToken);

        await context.Notifications
            .IgnoreQueryFilters()
            .Where(n => n.CreatedAtUtc < nowUtc.AddDays(-2))
            .ExecuteUpdateAsync(
                s => s.SetProperty(n => n.ReadAtUtc, n => (DateTime?)n.CreatedAtUtc.AddHours(1)),
                cancellationToken);
    }

    private static TaskItem CreateTask(
    Faker f, TaskTemplate template, List<string> userIds, DateOnly today, DateTime nowUtc)
    {
        // 0 = À faire, 1 = En cours, 2 = En validation, 3 = Résolue
        var steps = f.Random.WeightedRandom([0, 1, 2, 3], [0.35f, 0.30f, 0.15f, 0.20f]);

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{n}"] = f.Random.Int(120, 980).ToString(CultureInfo.InvariantCulture),
            ["{client}"] = f.Company.CompanyName(),
            ["{person}"] = f.Name.FirstName(),
            ["{version}"] = f.System.Semver(),
        };

        var at = nowUtc.AddDays(-(steps + 2)).AddHours(-f.Random.Int(1, 8));
        DateTime Next() => at = at.AddHours(f.Random.Int(2, 20));

        var createdById = f.PickRandom(userIds);

        // 0 = Bug, Extracte = 1, Analyse = 2, Task = 3, Story = 4
        var type = f.Random.WeightedRandom(
            [TaskType.Task, TaskType.Bug, TaskType.Story, TaskType.Analyse, TaskType.Extracte],
            [0.40f, 0.25f, 0.20f, 0.10f, 0.05f]);

        var task = TaskItem.Create(
            createdById,
            Fill(template.Title, values),
            template.Description is null ? null : Fill(template.Description, values),
            f.Random.Bool(0.8f) ? template.Priority : f.PickRandom<TaskPriority>(),
            DueDate(f, today, steps),
            createdAtUtc: at,
            type: type);

        AddChecklist(f, task, template.Checklist, steps);

        var others = userIds.Where(id => !string.Equals(id,createdById, StringComparison.Ordinal)).ToList();
        if (others.Count > 0 && f.Random.Bool(0.6f)) // 60 %
        {
            task.AssignTo(f.PickRandom(others), createdById, Next());
        }

        AdvanceStatus(task, steps, Next);

        return task;
    }

    private static string Fill(string text, Dictionary<string, string> values) =>
        values.Aggregate(text, (current, pair) => current.Replace(pair.Key, pair.Value, StringComparison.Ordinal));

    /// <summary>Tâches résolues : échéance passée ; autres : de « en retard » à « dans 3 semaines ». 25 % sans échéance.</summary>
    private static DateOnly? DueDate(Faker f, DateOnly today, int steps)
    {
        var offset = steps == WorkflowPath.Length ? f.Random.Int(-14, 0) : f.Random.Int(-5, 21);
        return today.AddDays(offset).OrNull(f, 0.25f);
    }

    /// <summary>Étapes cochées dans l'ordre, cohérentes avec l'avancement de la tâche.</summary>
    private static void AddChecklist(Faker f, TaskItem task, string[] items, int steps)
    {
        var doneCount = steps switch
        {
            0 => 0,
            1 => f.Random.Int(0, Math.Max(0, items.Length - 1)),
            _ => items.Length,
        };

        for (var i = 0; i < items.Length; i++)
        {
            var item = task.AddChecklistItem(items[i]);

            if (i < doneCount)
            {
                task.SetChecklistItemDone(item.Id, isDone: true);
            }
        }
    }

    /// <summary>
    /// Fait avancer la tâche dans le workflow, étape par étape.
    /// C'est l'assigné qui fait avancer le ticket ; à défaut, le créateur.
    /// </summary>
    private static void AdvanceStatus(TaskItem task, int steps, Func<DateTime> next)
    {
        var actorId = task.AssigneeId ?? task.CreatedById;

        for (var i = 0; i < steps; i++)
        {
            task.ChangeStatus(WorkflowPath[i], actorId, next());
        }
    }
}
