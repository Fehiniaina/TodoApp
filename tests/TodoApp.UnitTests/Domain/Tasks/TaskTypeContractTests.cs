using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Activity;
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
        [TaskType.Extraction] = 1,
        [TaskType.Analyse] = 2,
        [TaskType.Task] = 3,
        [TaskType.UserStory] = 4,
    };

    /// <summary>
    /// Noms PERSISTÉS dans task_activities.type. Renommer un membre = rendre illisible tout l'historique.
    /// Ajouter un type = ajouter une ligne ici.
    /// </summary>
    private static readonly string[] PersistedNames =
    [
        "Created",
        "Assigned",
        "StatusChanged",
        "AttachmentAdded",
        "AttachmentRemoved",
        "TypeChanged",
    ];

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
            [TaskType.Analyse, TaskType.Bug, TaskType.Extraction, TaskType.Task, TaskType.UserStory],
            TaskTypeExtensions.InDisplayOrder);
    }

    [Fact]
    public void Persisted_names_never_change()
    {
        Assert.Equal(
            PersistedNames.Order(StringComparer.Ordinal),
            Enum.GetNames<TaskActivityType>().Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Names_fit_the_column()
    {
        // HasMaxLength(40) dans TaskActivityConfiguration.
        Assert.All(Enum.GetNames<TaskActivityType>(), name => Assert.True(name.Length <= 40, name));
    }
}
