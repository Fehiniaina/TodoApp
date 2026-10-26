using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TodoApp.Features.Shared.Components;

public sealed partial class InlineTextEditor
{
    [Parameter, EditorRequired] public string? Value { get; set; }

    /// <summary>Nom du champ, pour l'accessibilité et les messages (« Le titre est obligatoire »).</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    /// <summary>Enregistre la valeur ; renvoie un message d'erreur, ou null si tout s'est bien passé.</summary>
    [Parameter, EditorRequired] public Func<string?, Task<string?>> OnSave { get; set; } = null!;

    [Parameter] public bool Multiline { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public int MaxLength { get; set; } = 200;
    [Parameter] public string Placeholder { get; set; } = "Ajouter…";
    [Parameter] public string? DisplayCssClass { get; set; }
    [Parameter] public string? InputCssClass { get; set; }

    private ElementReference _input;
    private string _draft = string.Empty;
    private string? _error;
    private bool _isEditing;
    private bool _isSaving;
    private bool _focusPending;

    private void StartEditing()
    {
        _draft = Value ?? string.Empty;
        _error = null;
        _isEditing = true;
        _focusPending = true;
    }

    private void Cancel()
    {
        _isEditing = false;
        _error = null;
    }

    private async Task SaveAsync()
    {
        var value = Normalize(_draft);

        if (Required && value is null)
        {
            _error = $"Le champ « {Label} » est obligatoire.";
            return;
        }

        // Rien n'a changé : on referme sans appel serveur, et sans ligne inutile dans l'historique.
        if (string.Equals(value, Normalize(Value), StringComparison.Ordinal))
        {
            _isEditing = false;
            return;
        }

        _isSaving = true;
        _error = null;
        try
        {
            _error = await OnSave(value);
            if (_error is null)
            {
                _isEditing = false;
            }
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (string.Equals(e.Key, "Escape", StringComparison.Ordinal))
        {
            Cancel();
        }
        else if (string.Equals(e.Key, "Enter", StringComparison.Ordinal) && (!Multiline || e.CtrlKey || e.MetaKey))
        {
            await SaveAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusPending)
        {
            _focusPending = false;
            await _input.FocusAsync();
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
