using Microsoft.AspNetCore.Components;

namespace TodoApp.Features.Tasks.Shared;

public sealed partial class TaskEditor
{
    [Parameter, EditorRequired] public TaskFormModel Model { get; set; } = null!;
    [Parameter] public string SubmitText { get; set; } = "Enregistrer";
    [Parameter] public bool IsSubmitting { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback OnValidSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>Champs additionnels rendus avant les boutons (optionnel).</summary>
    [Parameter] public RenderFragment? ExtraFields { get; set; }

    /// <summary>
    /// Affiche le champ Type. Faux en modification : le type se change depuis la page de détail
    /// (ChangeType + activité), pas via Update.
    /// </summary>
    [Parameter] public bool ShowType { get; set; } = true;
}
