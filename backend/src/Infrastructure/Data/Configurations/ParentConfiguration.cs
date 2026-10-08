using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Configurations;

public class ParentConfiguration : IEntityTypeConfiguration<Parent>
{
    public void Configure(EntityTypeBuilder<Parent> builder)
    {
        builder
            .HasMany(p => p.Children)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "parent_student",
                r => r.HasOne<Student>().WithMany().HasForeignKey("student_id"),
                l => l.HasOne<Parent>().WithMany().HasForeignKey("parent_id"),
                j => j.HasKey("parent_id", "student_id"));
        builder
            .Navigation(p => p.Children)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
