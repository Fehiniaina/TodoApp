using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Tags;

namespace TodoApp.Infrastructure.Configurations;

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        // La contrainte suit TagColor : à mettre à jour (avec une migration) si une couleur est ajoutée.
        builder.ToTable("tags", t => t.HasCheckConstraint("ck_tags_color", "color BETWEEN 0 AND 6"));

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Name).IsRequired().HasMaxLength(Tag.MaxNameLength);
        builder.Property(t => t.NormalizedName).IsRequired().HasMaxLength(Tag.MaxNameLength);

        // Garantie d'unicité côté base : deux créations simultanées du même nom ne passent pas toutes les deux.
        builder.HasIndex(t => t.NormalizedName).IsUnique();

        builder.Property(t => t.Color).IsRequired();
        builder.Property(t => t.CreatedById).IsRequired().HasMaxLength(450);
        builder.Property(t => t.CreatedAtUtc).IsRequired();
    }
}
