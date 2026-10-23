namespace TodoApp.Infrastructure.Email.Templating;

internal sealed record RenderedEmail(string Subject, string Html, string Text);
