using System.Collections.Frozen;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TodoApp.Infrastructure.Email.Templating;

internal sealed class RazorEmailRenderer(
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory,
    IEnumerable<EmailTemplateRegistration> registrations) : IEmailRenderer
{
    private readonly FrozenDictionary<Type, Type> _templates =
        registrations.ToFrozenDictionary(r => r.Model, r => r.Template);

    public async Task<RenderedEmail> RenderAsync(IEmailModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!_templates.TryGetValue(model.GetType(), out var templateType))
        {
            throw new InvalidOperationException(
                $"Aucun template email enregistré pour {model.GetType().Name}.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        await using var renderer = new HtmlRenderer(scope.ServiceProvider, loggerFactory);

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [nameof(EmailTemplate<IEmailModel>.Model)] = model,
            });

            var output = await renderer.RenderComponentAsync(templateType, parameters);
            return output.ToHtmlString();
        });

        return new RenderedEmail(model.Subject, html, model.PlainText);
    }
}
