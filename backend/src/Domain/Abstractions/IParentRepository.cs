using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Abstractions;

public interface IParentRepository : IRepository<Parent>
{
    /// <summary>Родитель вместе с детьми.</summary>
    Task<Parent?> GetWithChildrenAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Parent>> ListByChildAsync(Guid studentId, CancellationToken cancellationToken = default);
}
