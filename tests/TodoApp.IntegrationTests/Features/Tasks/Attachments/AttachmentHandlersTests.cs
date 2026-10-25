using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TodoApp.Common;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Activity;
using TodoApp.Domain.Tasks.Attachments;
using TodoApp.Features.Tasks.Attachments.Add;
using TodoApp.Features.Tasks.Attachments.Delete;
using TodoApp.Features.Tasks.Attachments.Download;
using TodoApp.Features.Tasks.Attachments.List;
using TodoApp.Infrastructure.Storage;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Features.Tasks.Attachments;

[Collection("Postgres")]
public class AttachmentHandlersTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Alice = "alice"; // créatrice
    private const string Bob = "bob";     // assigné
    private const string Carol = "carol"; // sans accès

    private static readonly byte[] Pdf = "%PDF-1.7\n contenu de test"u8.ToArray();
    private static readonly string[] expectedFiles = new[] { "second.pdf", "premier.pdf" };

    private readonly FakeTimeProvider _time =
        new(new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero));

    private readonly string _storageRoot =
        Path.Combine(Path.GetTempPath(), "todoapp-tests", Guid.NewGuid().ToString("N"));

    private TaskScenario Scenario => new(fixture, _time);

    private LocalFileStorage Storage =>
        new(Options.Create(new FileStorageOptions { RootPath = _storageRoot }), _time);

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(Alice, Bob, Carol);
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }

        return Task.CompletedTask;
    }

    // ── Ajout ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Assignee_can_add_a_valid_pdf()
    {
        var taskId = await CreateAssignedTaskAsync();

        var result = await AddAsync(Bob, taskId, "Devis final.pdf", Pdf);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.Equal(1, StoredFileCount());

        await using var db = fixture.CreateDbContext();
        var attachment = await db.TaskAttachments.SingleAsync();
        Assert.Equal("Devis final.pdf", attachment.FileName);
        Assert.Equal("application/pdf", attachment.ContentType);
        Assert.Equal(Pdf.Length, attachment.SizeBytes);
        Assert.True(await db.TaskActivities.AnyAsync(a => a.Type == TaskActivityType.AttachmentAdded));
    }

    [Fact]
    public async Task User_without_access_gets_not_found_and_nothing_is_written()
    {
        var taskId = await CreateAssignedTaskAsync();

        var result = await AddAsync(Carol, taskId, "devis.pdf", Pdf);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(0, StoredFileCount());
    }

    [Fact]
    public async Task Executable_disguised_as_pdf_is_rejected_and_nothing_is_written()
    {
        var taskId = await CreateAssignedTaskAsync();

        var result = await AddAsync(Alice, taskId, "facture.pdf", [0x4D, 0x5A, 0x90, 0x00]);

        Assert.Equal("Attachment.ContentMismatch", result.Error.Code);
        Assert.Equal(0, StoredFileCount());
    }

    [Fact]
    public async Task Real_size_is_enforced_whatever_the_browser_announces()
    {
        var taskId = await CreateAssignedTaskAsync();
        var tooBig = new byte[TaskAttachment.MaxSizeBytes + 1];
        "%PDF-"u8.CopyTo(tooBig);

        var result = await AddAsync(Alice, taskId, "gros.pdf", tooBig);

        Assert.Equal("Attachment.TooLarge", result.Error.Code);
        Assert.Equal(0, StoredFileCount());
    }

    [Fact]
    public async Task Cannot_exceed_the_attachment_limit()
    {
        var taskId = await CreateAssignedTaskAsync();
        for (var i = 0; i < TaskItem.MaxAttachments; i++)
        {
            Assert.True((await AddAsync(Alice, taskId, $"f{i}.pdf", Pdf)).IsSuccess);
        }

        var result = await AddAsync(Alice, taskId, "un-de-trop.pdf", Pdf);

        Assert.Equal("Attachment.LimitReached", result.Error.Code);
        Assert.Equal(TaskItem.MaxAttachments, StoredFileCount());
    }

    // ── Liste ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_returns_newest_first_with_uploader_name()
    {
        var taskId = await CreateAssignedTaskAsync();
        await AddAsync(Alice, taskId, "premier.pdf", Pdf);
        _time.Advance(TimeSpan.FromMinutes(1));
        await AddAsync(Bob, taskId, "second.pdf", Pdf);

        var result = await ListHandler(Bob).HandleAsync(taskId);

        Assert.Equal(expectedFiles, result.Value.Select(a => a.FileName));
        Assert.All(result.Value, a => Assert.False(string.IsNullOrWhiteSpace(a.UploadedByName)));
    }

    [Fact]
    public async Task List_of_an_inaccessible_task_returns_not_found()
    {
        var taskId = await CreateAssignedTaskAsync();

        var result = await ListHandler(Carol).HandleAsync(taskId);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    // ── Suppression ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Uploader_can_delete_and_the_file_is_removed()
    {
        var taskId = await CreateAssignedTaskAsync();
        var attachmentId = (await AddAsync(Bob, taskId, "devis.pdf", Pdf)).Value;

        var result = await DeleteHandler(Bob).HandleAsync(new DeleteTaskAttachmentCommand(taskId, attachmentId));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, StoredFileCount());

        await using var db = fixture.CreateDbContext();
        Assert.False(await db.TaskAttachments.AnyAsync());
        Assert.True(await db.TaskActivities.AnyAsync(a => a.Type == TaskActivityType.AttachmentRemoved));
    }

    [Fact]
    public async Task Task_creator_can_delete_an_attachment_added_by_the_assignee()
    {
        var taskId = await CreateAssignedTaskAsync();
        var attachmentId = (await AddAsync(Bob, taskId, "devis.pdf", Pdf)).Value;

        var result = await DeleteHandler(Alice).HandleAsync(new DeleteTaskAttachmentCommand(taskId, attachmentId));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Assignee_cannot_delete_an_attachment_added_by_the_creator()
    {
        var taskId = await CreateAssignedTaskAsync();
        var attachmentId = (await AddAsync(Alice, taskId, "devis.pdf", Pdf)).Value;

        var result = await DeleteHandler(Bob).HandleAsync(new DeleteTaskAttachmentCommand(taskId, attachmentId));

        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal(1, StoredFileCount());
    }

    // ── Téléchargement ──────────────────────────────────────────────────────

    [Fact]
    public async Task Visible_user_gets_the_original_content()
    {
        var taskId = await CreateAssignedTaskAsync();
        var attachmentId = (await AddAsync(Alice, taskId, "devis.pdf", Pdf)).Value;

        var result = await ContentHandler(Bob).HandleAsync(taskId, attachmentId);

        Assert.True(result.IsSuccess);
        await using var content = result.Value.Content;
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy);
        Assert.Equal(Pdf, copy.ToArray());
        Assert.Equal("devis.pdf", result.Value.FileName);
    }

    [Fact]
    public async Task User_without_access_cannot_download()
    {
        var taskId = await CreateAssignedTaskAsync();
        var attachmentId = (await AddAsync(Alice, taskId, "devis.pdf", Pdf)).Value;

        var result = await ContentHandler(Carol).HandleAsync(taskId, attachmentId);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Attachment_id_with_another_task_id_is_not_found()
    {
        var taskId = await CreateAssignedTaskAsync();
        var otherTaskId = await Scenario.CreateTaskAsync(Alice);
        var attachmentId = (await AddAsync(Alice, taskId, "devis.pdf", Pdf)).Value;

        var result = await ContentHandler(Alice).HandleAsync(otherTaskId, attachmentId);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Guid> CreateAssignedTaskAsync()
    {
        var taskId = await Scenario.CreateTaskAsync(Alice);
        await Scenario.AssignAsync(Alice, taskId, Bob);
        return taskId;
    }

    private async Task<Result<Guid>> AddAsync(string userId, Guid taskId, string fileName, byte[] content)
    {
        using var stream = new MemoryStream(content);
        var handler = new AddTaskAttachmentHandler(
            new TestDbContextFactory(fixture), new FakeCurrentUser(userId), Storage, _time,
            NullLogger<AddTaskAttachmentHandler>.Instance);

        return await handler.HandleAsync(new AddTaskAttachmentCommand(taskId, fileName, stream));
    }

    private GetTaskAttachmentsHandler ListHandler(string userId) =>
        new(new TestDbContextFactory(fixture), new FakeCurrentUser(userId));

    private DeleteTaskAttachmentHandler DeleteHandler(string userId) =>
        new(new TestDbContextFactory(fixture), new FakeCurrentUser(userId), Storage, _time,
            NullLogger<DeleteTaskAttachmentHandler>.Instance);

    private GetAttachmentContentHandler ContentHandler(string userId) =>
        new(new TestDbContextFactory(fixture), new FakeCurrentUser(userId), Storage,
            NullLogger<GetAttachmentContentHandler>.Instance);

    private int StoredFileCount() =>
        Directory.Exists(_storageRoot)
            ? Directory.GetFiles(_storageRoot, "*", SearchOption.AllDirectories).Length
            : 0;
}
