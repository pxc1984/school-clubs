using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Infrastructure.Data.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lesson");

        builder.HasKey(l => l.Id);
        builder
            .Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder
            .Property(l => l.Date)
            .HasColumnName("date");
        builder
            .Property(l => l.Time)
            .HasColumnName("time");

        builder
            .Property(l => l.ParentGroupId)
            .HasColumnName("parent_group_id");
        builder
            .HasOne(l => l.ParentGroup)
            .WithMany()
            .HasForeignKey(l => l.ParentGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(l => l.Tasks)
            .WithOne(t => t.Lesson)
            .HasForeignKey(t => t.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .Navigation(l => l.Tasks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasAuditColumns();
    }
}
