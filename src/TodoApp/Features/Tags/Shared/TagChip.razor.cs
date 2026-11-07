using Microsoft.AspNetCore.Components;

namespace TodoApp.Features.Tags.Shared;

public sealed partial class TagChip
{
    [Parameter, EditorRequired] public TagDto Tag { get; set; } = null!;

    /// <summary>Sans délégué : chip en lecture seule (liste des tâches).</summary>
    [Parameter] public EventCallback OnRemove { get; set; }
}
