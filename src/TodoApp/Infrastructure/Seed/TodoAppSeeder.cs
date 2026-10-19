using Bogus;

using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Domain.Tasks;

namespace TodoApp.Infrastructure.Seed;

public static class TodoAppSeeder
{
    public static async Task SeedAsync(
        IDbContextFactory<ApplicationDbContext> factory
    )
    {
        ArgumentNullException.ThrowIfNull(factory);

        await using var context = await factory.CreateDbContextAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var userIds = await context.Users.Select(u => u.Id).ToListAsync();
        if (userIds.Count == 0)
        {
            return; // No users registered yet — nothing to seed tasks against.
        }

        if (await context.Tasks.AnyAsync())
        {
            return; // Already seeded.
        }

        var taskFaker = new Faker<TaskItem>()
            .CustomInstantiator(f => TaskItem.Create(
                ownerId: f.PickRandom(userIds),
                title: f.Lorem.Sentence(3, 5),
                description: f.Lorem.Paragraph(),
                priority: f.PickRandom<TaskPriority>(),
                dueDate: today.AddDays(f.Random.Int(-7, 21)).OrNull(f, 0.25f)
            ));

        var tasks = taskFaker.Generate(10);

        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync();
    }
}
