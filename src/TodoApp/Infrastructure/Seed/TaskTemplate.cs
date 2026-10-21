using TodoApp.Domain.Tasks;

namespace TodoApp.Infrastructure.Seed;

internal sealed record TaskTemplate(
    string Title,
    string? Description,
    TaskPriority Priority,
    string[] Checklist);
