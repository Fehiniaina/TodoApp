using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Data;
using TodoApp.Domain.Notifications;

namespace TodoApp.Infrastructure.Configurations;

internal sealed class EmailDeliveryConfiguration : IEntityTypeConfiguration<EmailDelivery>
{
    public void Configure(EntityTypeBuilder<EmailDelivery> builder)
    {
        builder.ToTable("email_deliveries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.LastError).HasMaxLength(EmailDelivery.MaxErrorLength);

        builder.HasQueryFilter(QueryFilters.SoftDelete, e => !e.Notification.Task.IsDeleted);

        builder.HasIndex(e => e.NextAttemptAtUtc)
            .HasFilter("status = 'Pending'")
            .HasDatabaseName("ix_email_deliveries_pending");
    }
}
