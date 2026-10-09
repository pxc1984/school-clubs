using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Infrastructure.Data.Configurations;

public class LessonGradebookConfiguration : IEntityTypeConfiguration<LessonGradebook>
{
    public void Configure(EntityTypeBuilder<LessonGradebook> builder)
    {
        builder.ToTable("lesson_gradebook");

        builder.HasKey(g => g.Id);
        builder
            .Property(g => g.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Один журнал на занятие; lesson_id — альтернативный ключ, на который ссылаются записи журнала.
        builder
            .Property(g => g.LessonId)
            .HasColumnName("lesson_id");
        builder.HasAlternateKey(g => g.LessonId);
        builder
            .HasOne(g => g.Lesson)
            .WithOne()
            .HasForeignKey<LessonGradebook>(g => g.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(g => g.DataEntryId)
            .HasColumnName("data_entry_id");
        builder
            .HasOne(g => g.StoredFile)
            .WithMany()
            .HasForeignKey(g => g.DataEntryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .Property(g => g.PublishedAt)
            .HasColumnName("published_at");
        builder.Ignore(g => g.IsPublished);

        builder
            .HasMany(g => g.GradebookEntries)
            .WithOne()
            .HasForeignKey(e => e.LessonId)
            .HasPrincipalKey(g => g.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .Navigation(g => g.GradebookEntries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder
            .HasMany(g => g.Attendance)
            .WithOne()
            .HasForeignKey(a => a.LessonId)
            .HasPrincipalKey(g => g.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .Navigation(g => g.Attendance)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasAuditColumns();
    }
}
