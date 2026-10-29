using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.UnitTests.Domain.Tasks;

public sealed class TaskTypeContractTests
{
    /// <summary>
    /// Valeurs persistées. Modifier une ligne existante = corrompre les données.
    /// Ajouter un type = ajouter une ligne ici (prochain nombre libre).
    /// </summary>
    private static readonly Dictionary<TaskType, int> PersistedValues = new()
    {
        [TaskType.Bug] = 0,
        [TaskType.Extracte] = 1,
        [TaskType.Analyse] = 2,
        [TaskType.Task] = 3,
        [TaskType.Story] = 4,
    };

    [Fact]
    public void Persisted_values_never_change()
    {
        var actual = Enum.GetValues<TaskType>().ToDictionary(t => t, t => (int)t);

        Assert.Equal(PersistedValues, actual);
    }

    [Fact]
    public void Persisted_values_are_unique()
    {
        var values = Enum.GetValues<TaskType>().Select(t => (int)t).ToList();

        Assert.Equal(values.Count, values.Distinct().Count());
    }

    [Fact]
    public void Display_order_is_alphabetical_by_label()
    {
        Assert.Equal(
            [TaskType.Analyse, TaskType.Bug, TaskType.Extracte, TaskType.Story, TaskType.Task],
            TaskTypeExtensions.InDisplayOrder);
    }
}
