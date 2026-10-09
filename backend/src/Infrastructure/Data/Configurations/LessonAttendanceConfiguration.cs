using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Infrastructure.Data.Configurations;

/// <summary>Связь с журналом (по lesson_id) настраивается в <see cref="LessonGradebookConfiguration"/>.</summary>
public class LessonAttendanceConfiguration : IEntityTypeConfiguration<LessonAttendance>
{
    public void Configure(EntityTypeBuilder<LessonAttendance> builder)
    {
        builder.ToTable("lesson_attendance");

        builder.HasKey(a => new { a.LessonId, a.StudentId });

        builder
            .Property(a => a.LessonId)
            .HasColumnName("lesson_id");

        builder
            .Property(a => a.StudentId)
            .HasColumnName("student_id");
        builder
            .HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasAuditColumns();
    }
}
