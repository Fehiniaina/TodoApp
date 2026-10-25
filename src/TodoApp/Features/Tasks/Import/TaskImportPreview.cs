namespace TodoApp.Features.Tasks.Import;

internal sealed record TaskImportPreview(
    IReadOnlyList<TaskImportRow> Rows,
    IReadOnlyList<string> FileErrors)
{
    public int InvalidCount => Rows.Count(r => !r.IsValid);

    public bool CanImport => FileErrors.Count == 0 && Rows.Count > 0 && InvalidCount == 0;
}
