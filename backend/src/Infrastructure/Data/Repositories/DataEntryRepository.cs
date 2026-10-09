using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Data;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class DataEntryRepository(DbContext dbContext) : Repository<DataEntry>(dbContext), IDataEntryRepository
{
    public async Task<DataEntry?> FindByLocationAsync(S3Location location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        return await DbSet.FirstOrDefaultAsync(
            d => d.Location.Bucket == location.Bucket && d.Location.Key == location.Key,
            cancellationToken);
    }

    public async Task<IReadOnlyList<DataEntry>> ListByKeyPrefixAsync(string bucket,
        string keyPrefix,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(d => d.Location.Bucket == bucket && d.Location.Key.StartsWith(keyPrefix))
            .ToListAsync(cancellationToken);
    }
}
