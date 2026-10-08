using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities;

namespace SchoolClubs.Infrastructure.Data.Configurations;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("todo_item");

        builder.HasKey(t => t.Id);
        builder
            .Property(t => t.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder
            .Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();
        builder
            .Property(t => t.IsDone)
            .HasColumnName("is_done");

        builder.Property(t => t.Created).HasColumnName("created");
        builder.Property(t => t.CreatedBy).HasColumnName("created_by");
        builder.Property(t => t.LastModified).HasColumnName("last_modified");
        builder.Property(t => t.LastModifiedBy).HasColumnName("last_modified_by");
    }
}
