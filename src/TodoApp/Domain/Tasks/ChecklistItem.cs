namespace TodoApp.Domain.Tasks;

internal sealed class ChecklistItem
{
    private ChecklistItem() { }   // EF Core

    internal ChecklistItem(Guid id, string title, int position)
    {
        Id = id;
        Title = title;
        Position = position;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public bool IsDone { get; private set; }
    public int Position { get; private set; }

    // "internal" : seul l'agrégat (TaskItem) peut modifier une étape.
    internal void SetDone(bool isDone) => IsDone = isDone;
    internal void MoveTo(int position) => Position = position;
}