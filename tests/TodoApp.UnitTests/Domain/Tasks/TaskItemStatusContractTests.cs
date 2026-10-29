using System;
using System.Collections.Generic;
using System.Text;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Activity;
using TodoApp.Features.Tasks.Activity;

namespace TodoApp.UnitTests.Domain.Tasks;

public sealed class TaskItemStatusContractTests
{

    /// <summary>
    /// Valeurs persistées dans tasks.status ET dans l'historique (task_activities.old_value / new_value).
    /// Modifier une ligne existante = corrompre les tâches et tout leur historique.
    /// </summary>
    private static readonly Dictionary<TaskItemStatus, int> PersistedValues = new()
    {
        [TaskItemStatus.ToDo] = 0,
        [TaskItemStatus.InProgress] = 1,
        [TaskItemStatus.InValidation] = 2,
        [TaskItemStatus.Resolved] = 3,
        [TaskItemStatus.Done] = 4,
    };

    [Fact]
    public void Persisted_values_never_change()
    {
        var actual = Enum.GetValues<TaskItemStatus>().ToDictionary(s => s, s => (int)s);

        Assert.Equal(PersistedValues, actual);

    }

    [Theory]
    [InlineData("0", "1", "À faire", "En cours")]
    [InlineData("InProgress", "2", "InProgress", "En validation")]
    public void Describe_status_change(string oldValue, string newValue, string expectedOld, string expectedNew)
    {
        var dto = new TaskActivityDto(Guid.NewGuid(), TaskActivityType.StatusChanged, "Alice", oldValue, newValue, DateTime.UtcNow, null);

        var text = TaskActivityFormatter.Describe(dto);

        Assert.Contains(expectedOld, text, StringComparison.Ordinal);
        Assert.Contains(expectedNew, text, StringComparison.Ordinal);
    }
}
