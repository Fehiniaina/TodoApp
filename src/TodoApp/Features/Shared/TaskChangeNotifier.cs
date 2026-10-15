namespace TodoApp.Features.Shared;

/// <summary>
/// Singleton event hub for task changes. Handlers publish; interactive
/// components subscribe and re-render. No custom SignalR hub — Blazor Server
/// circuits carry the updates.
/// </summary>
internal sealed class TaskChangeNotifier
{
    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}
