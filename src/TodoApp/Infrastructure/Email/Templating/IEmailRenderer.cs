namespace TodoApp.Infrastructure.Email.Templating;

internal interface IEmailRenderer
{
    Task<RenderedEmail> RenderAsync(IEmailModel model);
}
