namespace TodoApp.Features.Tasks.Import;

internal sealed record TaskCsvParseResult(
    IReadOnlyList<ParsedTaskRow> Rows,
    IReadOnlyList<string> FileErrors);
