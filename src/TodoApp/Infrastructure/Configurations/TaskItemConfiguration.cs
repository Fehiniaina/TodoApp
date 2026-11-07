using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Data;
using TodoApp.Domain.Tags;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Recurrence;

namespace TodoApp.Infrastructure.Configurations;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        ConfigureProperties(builder);
        ConfigureSoftDelete(builder);
        ConfigureChecklist(builder);
        ConfigureRecurrence(builder);
        ConfigureAssignment(builder);
        ConfigureAttachments(builder);
        ConfigureTaskType(builder);
        ConfigureTags(builder);
    }

    private static void ConfigureProperties(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasKey(task => task.Id);

        builder.Property(task => task.CreatedById).IsRequired();
        builder.Property(task => task.Title).IsRequired().HasMaxLength(200);
        builder.Property(task => task.Description).HasMaxLength(2000);
        builder.Property(task => task.Priority).IsRequired();
        builder.Property(task => task.CreatedAtUtc).IsRequired();
        builder.Property(t => t.Status).IsRequired();
        builder.Ignore(t => t.IsCompleted);
        builder.Ignore(t => t.AllowedNextStatuses);
        builder.Ignore(t => t.DomainEvents);
    }

    private static void ConfigureSoftDelete(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasQueryFilter(QueryFilters.SoftDelete, task => !task.IsDeleted);

        builder.HasIndex(task => new { task.CreatedById, task.Priority })
               .HasFilter("is_deleted = false");

        builder.HasIndex(task => new { task.CreatedById, task.DeletedAt })
               .HasFilter("is_deleted = true");
    }

    private static void ConfigureChecklist(EntityTypeBuilder<TaskItem> builder)
    {
        builder.OwnsMany(task => task.Checklist, item =>
        {
            item.ToTable("checklist_items");
            item.WithOwner().HasForeignKey("TaskId");

            item.HasKey(i => i.Id);
            item.Property(i => i.Id).ValueGeneratedNever(); 

            item.Property(i => i.Title).IsRequired().HasMaxLength(200);
            item.Property(i => i.Position).IsRequired();

            item.HasIndex("TaskId", nameof(ChecklistItem.Position));
        });

        builder.Navigation(task => task.Checklist)
               .HasField("_checklist")
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureRecurrence(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasOne<RecurringTask>()
               .WithMany()
               .HasForeignKey(task => task.RecurringTaskId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(task => new { task.RecurringTaskId, task.DueDate })
               .IsUnique()
               .HasFilter("recurring_task_id IS NOT NULL");
    }

    private static void ConfigureAssignment(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasOne<ApplicationUser>()
               .WithMany()
               .HasForeignKey(t => t.AssigneeId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.AssigneeId)
               .HasFilter("is_deleted = false AND assignee_id IS NOT NULL");
    }

    private static void ConfigureAttachments(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasMany(t => t.Attachments)
            .WithOne(a => a.Task)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Attachments)
            .HasField("_attachments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureTaskType(EntityTypeBuilder<TaskItem> builder)
    {
        builder.Property(t => t.Type)
            .HasDefaultValue(TaskType.Task)
            // Sentinelle : valeur CLR qui signifie « non renseigné, laisse la base appliquer son défaut ».
            // Par défaut EF prend default(TaskType) = 0 = Bug : il n'enverrait donc jamais Bug à l'INSERT,
            // et la base le remplacerait par Task. -1 n'existe pas dans l'enum : EF envoie toujours la valeur.
            .HasSentinel((TaskType)(-1));
    }

    private static void ConfigureTags(EntityTypeBuilder<TaskItem> builder)
    {
        builder.OwnsMany(t => t.Tags, tag =>
        {
            tag.ToTable("task_tags");
            tag.WithOwner().HasForeignKey("TaskId");

            // Clé composite : une tâche ne peut pas porter deux fois le même tag (garanti par la base).
            tag.HasKey("TaskId", nameof(TaskTag.TagId));

            // Supprimer un tag le retire des tâches qui le portent.
            tag.HasOne<Tag>()
               .WithMany()
               .HasForeignKey(x => x.TagId)
               .OnDelete(DeleteBehavior.Cascade);

            // Pour « quelles tâches portent ce tag ? » (filtre, suppression en cascade).
            tag.HasIndex(x => x.TagId);
        });

        builder.Navigation(t => t.Tags)
               .HasField("_tags")
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
