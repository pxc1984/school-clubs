using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Configurations;

public class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin>
{
    public void Configure(EntityTypeBuilder<UserLogin> builder)
    {
        builder.ToTable("user_login");

        builder.HasKey(l => l.Id);
        builder
            .Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder
            .Property(l => l.UserId)
            .HasColumnName("user_id");
        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(l => l.Provider)
            .HasColumnName("provider")
            .HasMaxLength(UserLogin.ProviderMaxLength)
            .IsRequired();
        builder
            .Property(l => l.ProviderKey)
            .HasColumnName("provider_key")
            .HasMaxLength(254)
            .IsRequired();
        builder
            .HasIndex(l => new { l.Provider, l.ProviderKey })
            .IsUnique();

        builder
            .Property(l => l.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512);
        builder
            .Property(l => l.LastUsedAt)
            .HasColumnName("last_used_at");

        builder.HasAuditColumns();
    }
}
