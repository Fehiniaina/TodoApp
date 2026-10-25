using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Data;
using TodoApp.Domain.Tasks.Attachments;

namespace TodoApp.Infrastructure.Configurations;

internal sealed class TaskAttachmentConfiguration : IEntityTypeConfiguration<TaskAttachment>
{
    public void Configure(EntityTypeBuilder<TaskAttachment> builder)
    {
        builder.ToTable("task_attachments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.FileName).HasMaxLength(TaskAttachment.MaxFileNameLength).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.StorageKey).HasMaxLength(64).IsRequired();

        builder.HasIndex(a => a.StorageKey).IsUnique();
        builder.HasIndex(a => new { a.TaskId, a.UploadedAtUtc });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.UploadedById)
            .OnDelete(DeleteBehavior.SetNull);

        // Même règle que la checklist et l'historique : une tâche en corbeille masque ses pièces jointes.
        builder.HasQueryFilter(QueryFilters.SoftDelete, a => !a.Task.IsDeleted);
    }
}
