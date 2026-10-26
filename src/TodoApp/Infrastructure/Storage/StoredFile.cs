namespace TodoApp.Infrastructure.Storage;

internal sealed record StoredFile(string Key, DateTime LastModifiedUtc);
