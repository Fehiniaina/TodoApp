using System.Net.NetworkInformation;

namespace TodoApp.Features.Tasks.Attachments;

internal sealed class UploadState(string fileName)
{
    private Status _status = Status.Pending;
    public Guid Id { get; } = Guid.NewGuid();

    public string FileName { get; } = fileName;

    public string? Error { get; private set; }

    public bool Succeeded => _status == Status.Done;

    public string StatusText => _status switch
    {
        Status.Done => "Envoyé",
        Status.Failed => Error!,
        _ => "Envoi…",
    };

    public string CssClass => _status switch
    {
        Status.Done => "is-done",
        Status.Failed => "is-error",
        _ => "is-pending"
    };

    public void Fail(string error)
    {
        Error = error;
        _status = Status.Failed;
    }

    public void Succeed() => _status = Status.Done;

}
