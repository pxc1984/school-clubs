using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolClubs.Domain.Common;

namespace SchoolClubs.Infrastructure.Data.Configurations;

internal static class AuditableEntityConfigurationExtensions
{
    /// <summary>Маппинг колонок <see cref="IAuditableEntity"/>.</summary>
    public static void HasAuditColumns<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditableEntity
    {
        builder.Property(e => e.Created).HasColumnName("created");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.LastModified).HasColumnName("last_modified");
        builder.Property(e => e.LastModifiedBy).HasColumnName("last_modified_by");
    }
}
