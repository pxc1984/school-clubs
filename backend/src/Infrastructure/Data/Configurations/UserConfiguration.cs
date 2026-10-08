using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Configurations;

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
            .Property(u => u.Phone)
            .HasColumnName("phone")
            .HasMaxLength(32);
    }
}
