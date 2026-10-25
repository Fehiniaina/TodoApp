using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Attachments;
using TodoApp.Features.Shared;
using TodoApp.Features.Shared.Notifications;
using TodoApp.Features.Tasks.Attachments.Add;
using TodoApp.Features.Tasks.Attachments.Delete;
using TodoApp.Features.Tasks.Attachments.List;

namespace TodoApp.Features.Tasks.Attachments;

public sealed partial class TaskAttachmentsPanel
{
    /// <summary>Id du champ fichier : le bouton « Joindre » de la page l'ouvre via un label for.</summary>
    public const string InputId = "task-attachment-input";

    private static readonly string Accept = string.Join(',', AttachmentFileValidator.AllowedExtensions);

    [Parameter, EditorRequired] public Guid TaskId { get; set; }

    [Parameter, EditorRequired] public string TaskCreatorId { get; set; } = string.Empty;

    /// <summary>Après un ajout ou une suppression, pour que la page rafraîchisse l'historique.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    [Inject] private GetTaskAttachmentsHandler ListHandler { get; set; } = null!;
    [Inject] private AddTaskAttachmentHandler AddHandler { get; set; } = null!;
    [Inject] private DeleteTaskAttachmentHandler DeleteHandler { get; set; } = null!;
    [Inject] private ICurrentUser CurrentUser { get; set; } = null!;
    [Inject] private TimeProvider Time { get; set; } = null!;
    [Inject] private NotificationService Toasts { get; set; } = null!;
    [Inject] private ILogger<TaskAttachmentsPanel> Logger { get; set; } = null!;

    private readonly List<UploadState> _uploads = [];
    private AttachmentDto[]? _items;
    private string _userId = string.Empty;
    private DateTime _nowUtc;
    private bool _isDragOver;
    private bool _isUploading;
    private Guid? _confirmDeleteId;

    private bool IsFull => (_items?.Length ?? 0) >= TaskItem.MaxAttachments;

    protected override async Task OnInitializedAsync()
    {
        _userId = await CurrentUser.GetIdAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _nowUtc = Time.GetUtcNow().UtcDateTime;
        var result = await ListHandler.HandleAsync(TaskId);
        _items = result.IsSuccess ? result.Value : [];
    }

    private async Task OnFilesSelectedAsync(InputFileChangeEventArgs e)
    {
        _isDragOver = false;
        _uploads.Clear();

        IReadOnlyList<IBrowserFile> files;
        try
        {
            files = e.GetMultipleFiles(maximumFileCount: TaskItem.MaxAttachments);
        }
        catch (InvalidOperationException)
        {
            Toasts.Warning($"Sélectionnez au plus {TaskItem.MaxAttachments} fichiers à la fois.", "Trop de fichiers");
            return;
        }

        var remaining = TaskItem.MaxAttachments - (_items?.Length ?? 0);
        _isUploading = true;

        try
        {
            foreach (var file in files)
            {
                var upload = new UploadState(file.Name);
                _uploads.Add(upload);

                // Contrôles immédiats, sans rien envoyer : le serveur les refera de toute façon.
                if (remaining <= 0)
                {
                    upload.Fail($"Limite de {TaskItem.MaxAttachments} pièces jointes atteinte.");
                    continue;
                }

                if (file.Size > TaskAttachment.MaxSizeBytes)
                {
                    upload.Fail("Fichier trop volumineux (10 Mo maximum).");
                    continue;
                }

                StateHasChanged(); // affiche « Envoi… » pour ce fichier

                if (await UploadAsync(file, upload))
                {
                    remaining--;
                }
            }

            await LoadAsync();
            _uploads.RemoveAll(u => u.Succeeded); // ne garde à l'écran que les échecs

            await OnChanged.InvokeAsync();
        }
        finally
        {
            _isUploading = false;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Security",
        "S5693:Content length limit should be safe",
        Justification = "Limite métier de 10 Mo validée. Accès vérifié avant lecture, taille réelle mesurée côté serveur (ReadBoundedAsync), 20 fichiers maximum par tâche, envoi séquentiel.")]
    private async Task<bool> UploadAsync(IBrowserFile file, UploadState upload)
    {
        try
        {
            // maxAllowedSize : Blazor coupe le flux au-delà, même si la taille annoncée était fausse.
            await using var stream = file.OpenReadStream(TaskAttachment.MaxSizeBytes);
            var result = await AddHandler.HandleAsync(new AddTaskAttachmentCommand(TaskId, file.Name, stream));

            if (result.IsFailure)
            {
                upload.Fail(result.Error.Message);
                return false;
            }

            upload.Succeed();
            return true;
        }
        catch (IOException ex)
        {
            LogUploadFailed(Logger, ex, file.Name);
            upload.Fail("Envoi interrompu, réessayez.");
            return false;
        }
    }

    private bool CanDelete(AttachmentDto attachment) =>
        string.Equals(attachment.UploadedById, _userId, StringComparison.Ordinal)
        || string.Equals(TaskCreatorId, _userId, StringComparison.Ordinal);

    private async Task DeleteAsync(AttachmentDto attachment)
    {
        _confirmDeleteId = null;

        var result = await DeleteHandler.HandleAsync(new DeleteTaskAttachmentCommand(TaskId, attachment.Id));
        if (result.IsFailure)
        {
            Toasts.Warning(result.Error.Message, "Suppression impossible");
            return;
        }

        Toasts.Info($"« {attachment.FileName} » a été supprimée.");
        await LoadAsync();
        await OnChanged.InvokeAsync();
    }

    private static string Kind(AttachmentDto attachment) => attachment.ContentType switch
    {
        "application/pdf" => "pdf",
        "application/zip" => "zip",
        var type when type.StartsWith("image/", StringComparison.Ordinal) => "image",
        var type when type.Contains("wordprocessingml", StringComparison.Ordinal) => "doc",
        var type when type.Contains("spreadsheetml", StringComparison.Ordinal) => "sheet",
        var type when type.Contains("presentationml", StringComparison.Ordinal) => "slide",
        _ => "text",
    };

    private static string Badge(AttachmentDto attachment) =>
        Path.GetExtension(attachment.FileName).TrimStart('.').ToUpperInvariant();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Envoi de la pièce jointe {FileName} interrompu")]
    private static partial void LogUploadFailed(ILogger logger, Exception exception, string fileName);
}
