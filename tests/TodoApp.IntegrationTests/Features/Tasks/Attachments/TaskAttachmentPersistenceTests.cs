using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using TodoApp.Data;
using TodoApp.Domain.Tasks.Activity;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Features.Tasks.Attachments;

[Collection("Postgres")]
public class TaskAttachmentPersistenceTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Alice = "alice";

    private readonly FakeTimeProvider _time =
        new(new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero));

    private TaskScenario Scenario => new(fixture, _time);

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        await fixture.CreateUsersAsync(Alice);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Added_attachment_is_persisted_with_an_activity()
    {
        var taskId = await Scenario.CreateTaskAsync(Alice);

        await AddAttachmentAsync(taskId, "devis.pdf");

        await using var db = fixture.CreateDbContext();
        var attachment = await db.TaskAttachments.SingleAsync();
        Assert.Equal("devis.pdf", attachment.FileName);
        Assert.Equal(Alice, attachment.UploadedById);

        Assert.True(await db.TaskActivities.AnyAsync(a =>
            a.TaskId == taskId && a.Type == TaskActivityType.AttachmentAdded && a.NewValue == "devis.pdf"));
    }

    [Fact]
    public async Task Attachments_of_a_trashed_task_are_hidden()
    {
        var taskId = await Scenario.CreateTaskAsync(Alice);
        await AddAttachmentAsync(taskId, "devis.pdf");

        await Scenario.TrashAsync(Alice, taskId);

        await using var db = fixture.CreateDbContext();
        Assert.False(await db.TaskAttachments.AnyAsync());
        Assert.Equal(1, await db.TaskAttachments.IgnoreQueryFilters([QueryFilters.SoftDelete]).CountAsync());
    }

    [Fact]
    public async Task Purging_the_task_deletes_attachment_rows()
    {
        var taskId = await Scenario.CreateTaskAsync(Alice);
        await AddAttachmentAsync(taskId, "devis.pdf");

        await using var db = fixture.CreateDbContext();
        await db.Tasks.IgnoreQueryFilters().Where(t => t.Id == taskId).ExecuteDeleteAsync();

        Assert.False(await db.TaskAttachments.IgnoreQueryFilters().AnyAsync());
    }

    private async Task AddAttachmentAsync(Guid taskId, string fileName)
    {
        await using var db = fixture.CreateDbContext();

        var task = await db.Tasks
            .AsTracking()
            .Include(t => t.Attachments)
            .SingleAsync(t => t.Id == taskId);

        task.AddAttachment(fileName, "application/pdf", 1_024, $"2026/10/{Guid.NewGuid():N}", Alice, _time.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync();
    }
}
