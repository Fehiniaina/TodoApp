using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.ListTasks;

namespace TodoApp.UnitTests.Features.Tasks.ListTasks;

public sealed class TaskFilterUrlSlugContractTests
{
    /// <summary>Slugs PERSISTÉS dans les favoris et liens partagés. Ne jamais modifier une ligne existante.</summary>
    private static readonly Dictionary<TaskType, string> TypeSlugs = new()
    {
        [TaskType.Bug] = "bug",
        [TaskType.Extraction] = "extraction",
        [TaskType.Analyse] = "analyse",
        [TaskType.Task] = "task",
        [TaskType.UserStory] = "story",
    };

    private static readonly Dictionary<TaskPriority, string> PrioritySlugs = new()
    {
        [TaskPriority.Low] = "low",
        [TaskPriority.Medium] = "medium",
        [TaskPriority.High] = "high",
        [TaskPriority.Urgent] = "urgent",
    };

    [Fact]
    public void Type_slugs_never_change() =>
        Assert.Equal(TypeSlugs, TaskFilterUrl.TypeSlugs.All);

    [Fact]
    public void Priority_slugs_never_change() =>
        Assert.Equal(PrioritySlugs, TaskFilterUrl.PrioritySlugs.All);
}
