using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Entities.Data;

namespace SchoolClubs.Infrastructure.Data.Configurations;

public class DataEntryConfiguration : IEntityTypeConfiguration<DataEntry>
{
    public void Configure(EntityTypeBuilder<DataEntry> builder)
    {
        builder.ToTable("data_entry");

        builder.HasKey(d => d.Id);
        builder
            .Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.ComplexProperty(d => d.Location, location =>
        {
            location
                .Property(l => l.Bucket)
                .HasColumnName("s3_bucket")
                .HasMaxLength(63)
                .IsRequired();
            location
                .Property(l => l.Key)
                .HasColumnName("s3_key")
                .HasMaxLength(1024)
                .IsRequired();
        });

        builder
            .Property(d => d.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(255)
            .IsRequired();
        builder
            .Property(d => d.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(255)
            .IsRequired();
        builder
            .Property(d => d.SizeBytes)
            .HasColumnName("size_bytes");
        builder
            .Property(d => d.UploadedAt)
            .HasColumnName("uploaded_at");
    }
}
