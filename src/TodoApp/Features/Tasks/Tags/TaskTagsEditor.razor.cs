using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TodoApp.Domain.Tags;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared.Notifications;
using TodoApp.Features.Tags.SearchTags;
using TodoApp.Features.Tags.Shared;
using TodoApp.Features.Tasks.Tags.AddTagToTask;
using TodoApp.Features.Tasks.Tags.RemoveTagFromTask;

namespace TodoApp.Features.Tasks.Tags;

public sealed partial class TaskTagsEditor : IDisposable
{
    private const int DebounceMilliseconds = 250;

    [Inject] private SearchTagsHandler SearchHandler { get; set; } = null!;
    [Inject] private AddTagToTaskHandler AddHandler { get; set; } = null!;
    [Inject] private RemoveTagFromTaskHandler RemoveHandler { get; set; } = null!;
    [Inject] private NotificationService Toasts { get; set; } = null!;

    [Parameter, EditorRequired] public Guid TaskId { get; set; }

    [Parameter, EditorRequired] public IReadOnlyList<TagDto> Tags { get; set; } = [];

    /// <summary>Après un ajout ou un retrait réussi : le parent recharge la tâche et l'historique.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    private readonly string _inputId = "tags-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

    private string _input = string.Empty;
    private List<TagOption> _options = [];
    private int _activeIndex = -1;
    private bool _open;
    private bool _busy;
    private CancellationTokenSource? _searchCts;

    private sealed record TagOption(string Name, TagDto? Existing);

    private string ListId => _inputId + "-list";

    private bool CanAddMore => Tags.Count < TaskItem.MaxTags;

    private bool IsListVisible => _open && _options.Count > 0;

    private string? ActiveOptionId => IsListVisible && _activeIndex >= 0 ? OptionId(_activeIndex) : null;

    private string OptionId(int index) =>
        string.Create(CultureInfo.InvariantCulture, $"{_inputId}-option-{index}");

    private Task OnInputAsync(ChangeEventArgs e)
    {
        _input = e.Value as string ?? string.Empty;
        return SearchDebouncedAsync();
    }

    private Task OnFocusAsync() => SearchDebouncedAsync();

    private void Close()
    {
        _open = false;
        _activeIndex = -1;
    }

    /// <summary>
    /// Attend une courte pause dans la frappe avant d'interroger la base :
    /// une requête par pause, pas une par touche.
    /// </summary>
    private async Task SearchDebouncedAsync()
    {
        // Remplacer AVANT d'attendre : pendant l'await de CancelAsync, une autre frappe peut
        // arriver ; elle doit trouver la NOUVELLE recherche comme courante, pas l'ancienne.
        var previous = _searchCts;
        var cts = _searchCts = new CancellationTokenSource();

        if (previous is not null)
        {
            await previous.CancelAsync();
            previous.Dispose();
        }

        try
        {
            await Task.Delay(DebounceMilliseconds, cts.Token);
            var found = await SearchHandler.HandleAsync(new SearchTagsQuery(_input), cts.Token);

            _options = BuildOptions(found);
            _activeIndex = _options.Count > 0 ? 0 : -1;
            _open = true;
        }
        catch (OperationCanceledException)
        {
            // Une frappe plus récente a remplacé cette recherche.
        }
    }

    /// <summary>Suggestions sans les tags déjà posés, plus « Créer » si le nom saisi n'existe pas encore.</summary>
    private List<TagOption> BuildOptions(IReadOnlyList<TagDto> found)
    {
        var attached = Tags.Select(t => t.Id).ToHashSet();
        var options = found
            .Where(t => !attached.Contains(t.Id))
            .Select(t => new TagOption(t.Name, t))
            .ToList();

        if (Tag.IsValidName(_input))
        {
            var normalized = Tag.Normalize(_input);
            var known = found.Concat(Tags)
                .Any(t => string.Equals(Tag.Normalize(t.Name), normalized, StringComparison.Ordinal));

            if (!known)
            {
                options.Add(new TagOption(_input.Trim(), Existing: null));
            }
        }

        return options;
    }

    private Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowDown":
                Move(1);
                return Task.CompletedTask;
            case "ArrowUp":
                Move(-1);
                return Task.CompletedTask;
            case "Escape":
                Close();
                return Task.CompletedTask;
            case "Enter":
                return IsListVisible && _activeIndex >= 0
                    ? SelectAsync(_options[_activeIndex])
                    : AddAsync(_input);
            default:
                return Task.CompletedTask;
        }
    }

    private void Move(int delta)
    {
        if (_options.Count == 0)
        {
            return;
        }

        _open = true;
        _activeIndex = (_activeIndex + delta + _options.Count) % _options.Count;
    }

    private Task SelectAsync(TagOption option) => AddAsync(option.Existing?.Name ?? option.Name);

    private async Task AddAsync(string name)
    {
        if (_busy || !Tag.IsValidName(name))
        {
            return;
        }

        _busy = true;
        try
        {
            if (_searchCts is { } search)
            {
                await search.CancelAsync();
            }

            var result = await AddHandler.HandleAsync(new AddTagToTaskCommand(TaskId, name));
            if (result.IsFailure)
            {
                Toasts.Warning(result.Error.Message, "Ajout du tag impossible");
                return;
            }

            _input = string.Empty;
            _options = [];
            Close();

            await OnChanged.InvokeAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RemoveAsync(TagDto tag)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            var result = await RemoveHandler.HandleAsync(new RemoveTagFromTaskCommand(TaskId, tag.Id));
            if (result.IsFailure)
            {
                Toasts.Warning(result.Error.Message, "Retrait du tag impossible");
                return;
            }

            await OnChanged.InvokeAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    public void Dispose()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }

}
