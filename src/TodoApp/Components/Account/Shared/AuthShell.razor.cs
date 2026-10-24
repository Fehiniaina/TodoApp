using Microsoft.AspNetCore.Components;

namespace TodoApp.Components.Account.Shared;

public partial class AuthShell
{
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    [Parameter] public string? Lead { get; set; }

    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = null!;

    [Parameter] public RenderFragment? Footer { get; set; }
}
