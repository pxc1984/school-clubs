using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Access;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Configurations;

public class ChildInviteConfiguration : IEntityTypeConfiguration<ChildInvite>
{
    public void Configure(EntityTypeBuilder<ChildInvite> builder)
    {
        builder.ToTable("child_invite");

        builder.HasKey(i => i.Id);
        builder
            .Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder
            .Property(i => i.StudentId)
            .HasColumnName("student_id");
        builder
            .HasOne<Student>()
            .WithMany()
            .HasForeignKey(i => i.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(i => i.StudentId);

        builder
            .Property(i => i.Token)
            .HasColumnName("token")
            .HasMaxLength(ChildInvite.TokenLength)
            .IsRequired();
        builder.HasIndex(i => i.Token).IsUnique();

        builder
            .Property(i => i.ExpiresAt)
            .HasColumnName("expires_at");
        builder
            .Property(i => i.AcceptedAt)
            .HasColumnName("accepted_at");

        builder
            .Property(i => i.AcceptedByUserId)
            .HasColumnName("accepted_by_user_id");
        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.AcceptedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .Property(i => i.RevokedAt)
            .HasColumnName("revoked_at");

        builder.HasAuditColumns();
    }
}
