using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Configurations;

/// <summary>Иерархия профилей (Student, User) хранится в одной таблице (TPH).</summary>
public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("profile");

        builder
            .HasDiscriminator<string>("kind")
            .HasValue<Student>("student")
            .HasValue<User>("user");
        builder
            .Property("kind")
            .HasColumnName("kind")
            .HasMaxLength(16);

        builder.HasKey(p => p.Id);
        builder
            .Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder
            .Property(p => p.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();
        builder
            .Property(p => p.MiddleName)
            .HasColumnName("middle_name")
            .HasMaxLength(100)
            .IsRequired();
        builder
            .Property(p => p.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasAuditColumns();
    }
}
