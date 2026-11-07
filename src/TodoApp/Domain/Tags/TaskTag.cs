namespace TodoApp.Domain.Tasks;

/// <summary>Référence d'une tâche vers un tag (autre agrégat), par son identité uniquement.</summary>
internal sealed class TaskTag
{
    private TaskTag() { }   // EF Core

    internal TaskTag(Guid tagId) => TagId = tagId;

    public Guid TagId { get; private set; }
}
