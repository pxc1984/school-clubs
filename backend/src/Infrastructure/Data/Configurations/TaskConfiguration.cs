using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Task = SchoolClubs.Domain.Entities.Gradebook.Task;

namespace SchoolClubs.Infrastructure.Data.Configurations;

/// <summary>Связь с занятием настраивается в <see cref="LessonConfiguration"/>.</summary>
public class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> builder)
    {
        builder.ToTable("task");

        builder.HasKey(t => t.Id);
        builder
            .Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Без уникального индекса (lesson_id, order): Lesson.RemoveTask сдвигает номера,
        // и промежуточные UPDATE нарушали бы неотложенное ограничение.
        builder
            .Property(t => t.Order)
            .HasColumnName("order");
        builder
            .Property(t => t.LessonId)
            .HasColumnName("lesson_id");

        builder
            .Navigation(t => t.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
