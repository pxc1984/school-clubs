using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Access;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Configurations;

public class AccessRequestConfiguration : IEntityTypeConfiguration<AccessRequest>
{
    public void Configure(EntityTypeBuilder<AccessRequest> builder)
    {
        builder.ToTable("access_request");

        builder.HasKey(r => r.Id);
        builder
            .Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder
            .Property(r => r.UserId)
            .HasColumnName("user_id");
        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(r => r.GroupId)
            .HasColumnName("group_id");
        builder
            .HasOne<Group>()
            .WithMany()
            .HasForeignKey(r => r.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(r => r.ChildLastName)
            .HasColumnName("child_last_name")
            .HasMaxLength(100)
            .IsRequired();
        builder
            .Property(r => r.ChildFirstName)
            .HasColumnName("child_first_name")
            .HasMaxLength(100)
            .IsRequired();
        builder
            .Property(r => r.ChildMiddleName)
            .HasColumnName("child_middle_name")
            .HasMaxLength(100);
        builder
            .Property(r => r.Grade)
            .HasColumnName("grade")
            .HasMaxLength(Student.GradeMaxLength);
        builder
            .Property(r => r.Relation)
            .HasColumnName("relation")
            .HasConversion<string>()
            .HasMaxLength(16);
        builder
            .Property(r => r.Comment)
            .HasColumnName("comment")
            .HasMaxLength(AccessRequest.CommentMaxLength);
        builder
            .Property(r => r.ConsentAcceptedAt)
            .HasColumnName("consent_accepted_at");

        builder
            .Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16);
        builder.HasIndex(r => new { r.Status, r.Created });
        builder.HasIndex(r => r.UserId);

        builder
            .Property(r => r.StudentId)
            .HasColumnName("student_id");
        builder
            .HasOne<Student>()
            .WithMany()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .Property(r => r.RejectReason)
            .HasColumnName("reject_reason")
            .HasMaxLength(AccessRequest.RejectReasonMaxLength);
        builder
            .Property(r => r.DecidedAt)
            .HasColumnName("decided_at");
        builder.Ignore(r => r.IsPending);

        builder.HasAuditColumns();
    }
}
