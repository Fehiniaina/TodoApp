using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Data;
using TodoApp.Domain.Notifications;

namespace TodoApp.Infrastructure.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.RecipientId).IsRequired();
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(50);
        builder.Ignore(n => n.IsRead);

        builder.HasOne(n => n.Task)
            .WithMany()
            .HasForeignKey(n => n.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(n => n.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(QueryFilters.SoftDelete, n => !n.Task.IsDeleted);

        builder.HasIndex(n => new { n.RecipientId, n.CreatedAtUtc })
            .IsDescending(false, true);

        builder.HasIndex(n => n.RecipientId)
            .HasFilter("read_at_utc IS NULL")
            .HasDatabaseName("ix_notifications_recipient_id_unread");

        builder.HasOne(n => n.Email)
            .WithOne(e => e.Notification)
            .HasForeignKey<EmailDelivery>(e => e.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
