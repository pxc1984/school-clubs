using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("group");

        builder.HasKey(g => g.Id);
        builder
            .Property(g => g.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder
            .Property(g => g.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();
        builder
            .Property(g => g.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);
        builder
            .Property(g => g.IsArchived)
            .HasColumnName("is_archived");

        builder
            .HasMany(g => g.Teachers)
            .WithMany(t => t.LinkedGroups)
            .UsingEntity<Dictionary<string, object>>(
                "group_teacher",
                r => r.HasOne<User>().WithMany().HasForeignKey("teacher_id"),
                l => l.HasOne<Group>().WithMany().HasForeignKey("group_id"),
                j => j.HasKey("group_id", "teacher_id"));
        builder
            .Navigation(g => g.Teachers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder
            .HasMany(g => g.Students)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "group_student",
                r => r.HasOne<Student>().WithMany().HasForeignKey("student_id"),
                l => l.HasOne<Group>().WithMany().HasForeignKey("group_id"),
                j => j.HasKey("group_id", "student_id"));
        builder
            .Navigation(g => g.Students)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasAuditColumns();
    }
}
