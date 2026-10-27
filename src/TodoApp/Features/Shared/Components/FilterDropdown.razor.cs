using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TodoApp.Features.Shared.Components;

#pragma warning disable VSSpell001 // Spell Check
public sealed partial class FilterDropdown
#pragma warning restore VSSpell001 // Spell Check
{
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    [Parameter] public int ActiveCount { get; set; }

    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = null!;

    [Parameter] public EventCallback OnClear { get; set; }

    private bool _isOpen;

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (string.Equals(e.Key, "Escape", StringComparison.Ordinal))
        {
            _isOpen = false;
        }
    }
}
