using SchoolClubs.Domain.Entities.Data;

namespace SchoolClubs.Domain.Abstractions;

public interface IDataEntryRepository : IRepository<DataEntry>
{
    Task<DataEntry?> FindByLocationAsync(S3Location location, CancellationToken cancellationToken = default);

    /// <summary>Файлы бакета, ключ которых начинается с <paramref name="keyPrefix"/>.</summary>
    Task<IReadOnlyList<DataEntry>> ListByKeyPrefixAsync(string bucket,
        string keyPrefix,
        CancellationToken cancellationToken = default);
}
