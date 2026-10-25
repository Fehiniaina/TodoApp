using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;
using TodoApp.Infrastructure.Storage;

namespace TodoApp.UnitTests.Infrastructure.Storage;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "todoapp-tests", Guid.NewGuid().ToString("N"));
    private readonly LocalFileStorage _storage;

    private readonly FakeTimeProvider _time =
        new(new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero));

    public LocalFileStorageTests()
    {
        _storage = new LocalFileStorage(Options.Create(new FileStorageOptions { RootPath = _root }), _time);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Save_then_read_returns_the_same_content()
    {
        var key = await _storage.SaveAsync(new MemoryStream("bonjour"u8.ToArray()), CancellationToken.None);

        await using var stream = await _storage.OpenReadAsync(key, CancellationToken.None);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        Assert.Equal("bonjour", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Key_is_year_month_guid_and_leaves_no_temporary_file()
    {
        var key = await _storage.SaveAsync(new MemoryStream([1, 2, 3]), CancellationToken.None);

        Assert.Matches(@"^2026/10/[0-9a-f]{32}$", key);
        Assert.Empty(Directory.GetFiles(_root, "*.uploading", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Delete_is_idempotent_and_read_after_delete_returns_null()
    {
        var key = await _storage.SaveAsync(new MemoryStream([1]), CancellationToken.None);

        await _storage.DeleteAsync(key, CancellationToken.None);
        await _storage.DeleteAsync(key, CancellationToken.None);

        Assert.Null(await _storage.OpenReadAsync(key, CancellationToken.None));
    }

    [Theory]
    [InlineData("../../appsettings.json")]
    [InlineData("2026/10/../../../secret")]
    [InlineData("C:/Windows/win.ini")]
    [InlineData("2026/10/NOT-A-GUID")]
    public async Task Invalid_keys_are_rejected(string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.OpenReadAsync(key, CancellationToken.None));
    }
}
