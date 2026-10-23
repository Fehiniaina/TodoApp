namespace TodoApp.Infrastructure.Email.Templating;

public interface IEmailModel
{
    string Subject { get; }

    string PlainText { get; }
}
