using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Configurations;

/// <summary>Связь с группами (group_teacher) настраивается в <see cref="GroupConfiguration"/>.</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Колонка nullable на уровне таблицы: у учеников (тот же TPH) почты нет.
        builder
            .Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(254)
            .HasConversion(email => email.Address, address => new MailAddress(address));
        // Не уникальный: в домене есть общий адрес-заглушка по умолчанию.
        builder.HasIndex(u => u.Email);

        builder
            .Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(User.UsernameMaxLength);
        // Ник хранится в нижнем регистре (см. User.ChangeUsername); NULL в уникальном индексе не конфликтуют.
        builder.HasIndex(u => u.Username).IsUnique();

        builder
            .Property(u => u.Phone)
            .HasColumnName("phone")
            .HasMaxLength(32);

        builder
            .Property(u => u.SecurityStamp)
            .HasColumnName("security_stamp")
            .HasMaxLength(32);
        builder
            .Property(u => u.IsBlocked)
            .HasColumnName("is_blocked");
        builder
            .Property(u => u.ConsentAcceptedAt)
            .HasColumnName("consent_accepted_at");
        builder
            .Property(u => u.ConsentVersion)
            .HasColumnName("consent_version")
            .HasMaxLength(32);
        builder
            .Property(u => u.ConsentIp)
            .HasColumnName("consent_ip")
            .HasMaxLength(45);

        // Публичное Roles — проекция; хранится приватная коллекция _roles.
        builder.Ignore(u => u.Roles);
        builder.OwnsMany<UserRoleGrant>("_roles", roles =>
        {
            roles.ToTable("user_role");
            roles.WithOwner().HasForeignKey("user_id");
            roles
                .Property(r => r.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .HasMaxLength(16);
            roles.HasKey("user_id", nameof(UserRoleGrant.Role));
        });

        builder
            .HasMany(u => u.Children)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "user_child",
                r => r.HasOne<Student>().WithMany().HasForeignKey("student_id"),
                l => l.HasOne<User>().WithMany().HasForeignKey("user_id"),
                j => j.HasKey("user_id", "student_id"));
        builder
            .Navigation(u => u.Children)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder
            .Navigation(u => u.LinkedGroups)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
