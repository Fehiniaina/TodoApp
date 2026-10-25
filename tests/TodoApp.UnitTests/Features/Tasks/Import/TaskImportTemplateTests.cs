using System;
using System.Collections.Generic;
using System.Text;
using TodoApp.Features.Tasks.Import;

namespace TodoApp.UnitTests.Features.Tasks.Import;

public sealed class TaskImportTemplateTests
{
    [Fact]
    public async Task Template_is_accepted_by_the_parser_without_errors()
    {
        using var stream = new MemoryStream(TaskImportTemplate.Create());

        var result = await TaskCsvParser.ParseAsync(stream, CancellationToken.None);

        Assert.Empty(result.FileErrors);
        Assert.Equal(3, result.Rows.Count);
        Assert.All(result.Rows, row => Assert.Empty(row.Errors));
    }

    [Fact]
    public void Template_starts_with_utf8_bom()
    {
        var bytes = TaskImportTemplate.Create();

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
    }
}
