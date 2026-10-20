using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Recurrence;

namespace TodoApp.Infrastructure.Configurations;

internal sealed class RecurringTaskConfiguration : IEntityTypeConfiguration<RecurringTask>
{
    public void Configure(EntityTypeBuilder<RecurringTask> builder)
    {
        builder.ToTable("recurring_tasks");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.OwnerId).IsRequired();
        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.TimeZoneId).IsRequired().HasMaxLength(64);

        builder.ComplexProperty(s => s.Rule, rule =>
        {
            rule.Property(r => r.Frequency).IsRequired();
            rule.Property(r => r.Interval).IsRequired();
            rule.Property(r => r.DaysOfWeek).IsRequired();
            rule.Property(r => r.DayOfMonth);
        });

        builder.HasIndex(s => s.OwnerId);
        builder.HasIndex(s => s.NextOccurrence).HasFilter("is_stopped = false");
    }
}
