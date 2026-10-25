namespace TodoApp.Domain.Tasks.Attachments;

internal sealed class TaskAttachment
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;
    public const int MaxFileNameLength = 255;

    // EF Core
    private TaskAttachment()
    {
        FileName = null!;
        ContentType = null!;
        StorageKey = null!;
    }

    private TaskAttachment(
        Guid taskId,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        string uploadedById,
        DateTime uploadedAtUtc)
    {
        Id = Guid.CreateVersion7();
        TaskId = taskId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        UploadedById = uploadedById;
        UploadedAtUtc = uploadedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public TaskItem Task { get; private set; } = null!;
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; }

    /// <summary>Null seulement si l'utilisateur a été supprimé depuis.</summary>
    public string? UploadedById { get; private set; }

    public DateTime UploadedAtUtc { get; private set; }

    internal static TaskAttachment Create(
        Guid taskId,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        string uploadedById,
        DateTime uploadedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(uploadedById);

        if (fileName.Length > MaxFileNameLength)
        {
            throw new ArgumentOutOfRangeException(nameof(fileName), "Le nom du fichier est trop long.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sizeBytes, MaxSizeBytes);

        return new TaskAttachment(taskId, fileName, contentType, sizeBytes, storageKey, uploadedById, uploadedAtUtc);
    }
}
