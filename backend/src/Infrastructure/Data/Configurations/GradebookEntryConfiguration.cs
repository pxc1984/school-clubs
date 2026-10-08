using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Infrastructure.Data.Configurations;

/// <summary>Связь с журналом (по lesson_id) настраивается в <see cref="LessonGradebookConfiguration"/>.</summary>
public class GradebookEntryConfiguration : IEntityTypeConfiguration<GradebookEntry>
{
    public void Configure(EntityTypeBuilder<GradebookEntry> builder)
    {
        builder.ToTable("gradebook_entry");

        // Задача принадлежит одному занятию, поэтому (задача, ученик) уникальна глобально.
        builder.HasKey(e => new { e.TaskId, e.StudentId });

        builder
            .Property(e => e.LessonId)
            .HasColumnName("lesson_id");
        builder
            .HasOne(e => e.Lesson)
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(e => e.StudentId)
            .HasColumnName("student_id");
        builder
            .HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(e => e.TaskId)
            .HasColumnName("task_id");
        builder
            .HasOne(e => e.Task)
            .WithMany(t => t.Entries)
            .HasForeignKey(e => e.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Ignore(e => e.IsSolved);
        builder
            .Property(e => e.Comment)
            .HasColumnName("comment")
            .HasMaxLength(GradebookEntry.CommentMaxLength);

        // Статистика ученика по занятиям.
        builder.HasIndex(e => new { e.StudentId, e.LessonId });

        builder.HasAuditColumns();
    }
}
