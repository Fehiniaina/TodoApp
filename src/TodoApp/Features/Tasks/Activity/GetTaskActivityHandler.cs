using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks.Activity;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;
using TodoApp.Features.Users;
using TodoApp.Infrastructure.Configurations;

namespace TodoApp.Features.Tasks.Activity;

internal sealed class GetTaskActivityHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser) : IHandler
{
    private const string SystemActor = "Système";

    public async Task<Result<IReadOnlyList<TaskActivityDto>>> HandleAsync(
        Guid taskId, CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        if (!await db.Tasks.VisibleTo(userId).AnyAsync(t => t.Id == taskId, cancellationToken))
        {
            return Error.NotFound("Task.NotFound", "Cette tâche n'existe pas ou a été supprimée.");
        }

        var activities = await db.TaskActivities
            .Where(a => a.TaskId == taskId)
            .OrderByDescending(a => a.OccurredAtUtc)
            .ThenByDescending(a => EF.Property<long>(a, TaskActivityConfiguration.SequenceProperty))
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

        var names = await LoadUserNamesAsync(db, activities, cancellationToken);

        return activities.Select(a => ToDto(a, names)).ToList();
    }

    private static TaskActivityDto ToDto(TaskActivity a, Dictionary<string, string> names)
    {
        string? Resolve(string? value) =>
            a.Type == TaskActivityType.Assigned && value is not null
                ? names.GetValueOrDefault(value, "Utilisateur supprimé")
                : value;

        var actor = a.ActorId is null ? SystemActor : names.GetValueOrDefault(a.ActorId, "Utilisateur supprimé");

        return new TaskActivityDto(a.Id, a.Type, actor, Resolve(a.OldValue), Resolve(a.NewValue), a.OccurredAtUtc, ActorId: a.ActorId);
    }

    /// <summary>Une seule requête pour tous les noms : acteurs + assignés anciens et nouveaux.</summary>
    private static async Task<Dictionary<string, string>> LoadUserNamesAsync(
        ApplicationDbContext db, List<TaskActivity> activities, CancellationToken cancellationToken)
    {
        var ids = activities
            .SelectMany(a => a.Type == TaskActivityType.Assigned
                ? new[] { a.ActorId, a.OldValue, a.NewValue }
                : new[] { a.ActorId })
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var users = await db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(
            u => u.Id,
            u => UserSummary.ToDisplayName(u.FirstName, u.LastName, u.Email),
            StringComparer.Ordinal);
    }
}
