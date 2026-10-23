using Microsoft.AspNetCore.Components;

namespace TodoApp.Infrastructure.Email.Templating;

public abstract class EmailTemplate<TModel> : ComponentBase
    where TModel : IEmailModel
{
    [Parameter, EditorRequired]
    public TModel Model { get; set; } = default!;
}
