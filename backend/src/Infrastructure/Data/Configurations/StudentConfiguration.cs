using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder
            .Property(s => s.Grade)
            .HasColumnName("grade")
            .HasMaxLength(Student.GradeMaxLength);
    }
}
