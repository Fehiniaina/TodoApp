using System;
using System.Collections.Generic;
using System.Text;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.CreateTask;

namespace TodoApp.IntegrationTests.Infrastructure;

/// <summary>
/// Constructeurs de commandes pour les tests : valeurs par défaut valides,
/// on ne précise que ce qui importe au test.
/// </summary>
internal static class TaskCommands
{
    public static CreateTaskCommand Create(
        string title = "A task",
        string? description = null,
        TaskPriority priority = TaskPriority.Medium,
        DateOnly? dueDate = null) =>
        new(title, description, priority, dueDate);
}
