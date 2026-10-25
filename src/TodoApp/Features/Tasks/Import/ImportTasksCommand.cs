namespace TodoApp.Features.Tasks.Import;

internal sealed record ImportTasksCommand(IReadOnlyList<TaskImportRow> Rows);
