using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Configurations;

/// <summary>Связь с группами (group_teacher) настраивается в <see cref="GroupConfiguration"/>.</summary>
public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder
            .Navigation(t => t.LinkedGroups)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
