using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Notifications;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Activity;
using TodoApp.Domain.Tasks.Attachments;
using TodoApp.Domain.Tasks.Recurrence;
using TodoApp.Infrastructure.Configurations;

namespace TodoApp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    // internal: TaskItem is an internal domain entity; tests access it via InternalsVisibleTo.
    internal DbSet<TaskItem> Tasks => Set<TaskItem>();
    internal DbSet<RecurringTask> RecurringTasks => Set<RecurringTask>();
    internal DbSet<TaskActivity> TaskActivities => Set<TaskActivity>();
    internal DbSet<Notification> Notifications => Set<Notification>();
    internal DbSet<EmailDelivery> EmailDeliveries => Set<EmailDelivery>();
    internal DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);
        builder.HasPostgresEnum<RecurrenceFrequency>(name: "recurrence_frequency");
        builder.ApplyConfigurationsFromAssembly(typeof(TaskItemConfiguration).Assembly);
    }
}
