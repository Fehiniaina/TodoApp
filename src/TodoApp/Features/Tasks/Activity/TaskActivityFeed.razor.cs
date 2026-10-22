using System.Globalization;
using Microsoft.AspNetCore.Components;
using TodoApp.Features.Shared;
using TodoApp.Features.Shared.Time;

namespace TodoApp.Features.Tasks.Activity;

public sealed partial class TaskActivityFeed : IDisposable
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    [Inject] private GetTaskActivityHandler Handler { get; set; } = null!;
    [Inject] private TaskChangeNotifier Notifier { get; set; } = null!;
    [Inject] private IUserClock Clock { get; set; } = null!;

    [Parameter, EditorRequired] public Guid TaskId { get; set; }

    private IReadOnlyList<TaskActivityDto>? _items;

    protected override async Task OnInitializedAsync()
    {
        Notifier.Changed += OnTasksChanged;
        await LoadAsync();
    }

    public void Dispose() => Notifier.Changed -= OnTasksChanged;

    private async Task LoadAsync()
    {
        var result = await Handler.HandleAsync(TaskId);
        _items = result.IsSuccess ? result.Value : [];
    }

    private void OnTasksChanged(object? sender, EventArgs e) =>
        _ = InvokeAsync(async () =>
        {
            try
            {
                await LoadAsync();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        });

    private string FormatTime(DateTime utc) => Clock.ToLocal(utc).ToString("d MMM 'à' HH:mm", French);
}
