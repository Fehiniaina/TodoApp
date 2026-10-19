using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Data;
using TodoApp.Domain.Tasks;

namespace TodoApp.Infrastructure.Configurations;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasKey(task => task.Id);

        builder.Property(task => task.OwnerId)
            .IsRequired();

        builder.Property(task => task.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(task => task.Description)
            .HasMaxLength(2000);

        builder.Property(task => task.Priority)
            .IsRequired();

        builder.Property(task => task.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(task => task.Id)
            .IsUnique();
        builder.HasIndex(task => new { task.OwnerId , task.Priority}); // composite index

        // Filtre nommé (EF Core 10) : on peut le désactiver seul, sans toucher à d'éventuels autres filtres.
        builder.HasQueryFilter(QueryFilters.SoftDelete, task => !task.IsDeleted);

        // Les requêtes courantes ne portent que sur les tâches actives : index partiel, plus petit et plus rapide.
        builder.HasIndex(task => new { task.OwnerId, task.Priority })
               .HasFilter("is_deleted = false");

        // Pour la corbeille et la purge.
        builder.HasIndex(task => new { task.OwnerId, task.DeletedAt })
               .HasFilter("is_deleted = true");

        builder.OwnsMany(task => task.Checklist, item =>
        {
            item.ToTable("checklist_items");

            // FK "task_id" (propriété fantôme) → tasks.id, ON DELETE CASCADE.
            item.WithOwner().HasForeignKey("TaskId");

            item.HasKey(i => i.Id);

            // Indispensable : la clé est générée côté C# (Guid v7).
            item.Property(i => i.Id).ValueGeneratedNever();

            item.Property(i => i.Title).IsRequired().HasMaxLength(200);
            item.Property(i => i.Position).IsRequired();

            item.HasIndex("TaskId", nameof(ChecklistItem.Position));
        });

        builder.Navigation(task => task.Checklist)
           .HasField("_checklist")
           .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
