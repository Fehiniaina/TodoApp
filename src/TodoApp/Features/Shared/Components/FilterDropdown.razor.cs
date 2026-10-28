using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TodoApp.Features.Shared.Components;

/// <summary>
/// Bouton de filtre façon Jira : libellé + chevron + compteur, panneau déroulant.
/// Fermeture : clic extérieur (backdrop transparent) ou Échap. Reste ouvert pendant
/// les sélections multiples (l'instance survit à la navigation de filtre).
/// </summary>
public sealed partial class FilterDropdown
{
    private readonly string _panelId = "flt-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
    private bool _open;

    [Parameter] public string Label { get; set; } = string.Empty;

    [Parameter] public int ActiveCount { get; set; }

    [Parameter] public EventCallback OnClear { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Contenu personnalisé du bouton (remplace libellé + chevron).</summary>
    [Parameter] public RenderFragment? Trigger { get; set; }

    [Parameter] public string? TriggerCssClass { get; set; }

    [Parameter] public string? AriaLabel { get; set; }

    [Parameter] public string? Title { get; set; }

    private void Toggle() => _open = !_open;

    private void Close() => _open = false;

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (string.Equals(e.Key, "Escape", StringComparison.Ordinal))
        {
            _open = false;
        }
    }
}
