using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Data;
using TodoApp.Domain.Tasks.Activity;

namespace TodoApp.Infrastructure.Configurations;

internal sealed class TaskActivityConfiguration : IEntityTypeConfiguration<TaskActivity>
{
    /// <summary>Ordre d'insertion strict, pour départager les activités de même horodatage.</summary>
    internal const string SequenceProperty = "Sequence";

    public void Configure(EntityTypeBuilder<TaskActivity> builder)
    {
        builder.ToTable("task_activities");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(40).IsRequired();

        builder.Property(a => a.ActorId).HasMaxLength(450);
        builder.Property(a => a.OldValue).HasMaxLength(500);
        builder.Property(a => a.NewValue).HasMaxLength(500);
        builder.Property(a => a.OccurredAtUtc).IsRequired();

        builder.HasOne(a => a.Task)
               .WithMany()
               .HasForeignKey(a => a.TaskId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(QueryFilters.SoftDelete, a => !a.Task.IsDeleted);

        builder.Property<long>(SequenceProperty)
            .UseIdentityByDefaultColumn();


        builder.HasIndex(nameof(TaskActivity.TaskId), nameof(TaskActivity.OccurredAtUtc), SequenceProperty);
    }
}
