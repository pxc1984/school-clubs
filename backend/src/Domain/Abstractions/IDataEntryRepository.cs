using SchoolClubs.Domain.Entities.Data;

namespace SchoolClubs.Domain.Abstractions;

public interface IDataEntryRepository : IRepository<DataEntry>
{
    Task<DataEntry?> FindByLocationAsync(S3Location location, CancellationToken cancellationToken = default);
}
