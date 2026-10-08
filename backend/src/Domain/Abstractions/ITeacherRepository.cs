using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Abstractions;

public interface ITeacherRepository : IRepository<Teacher>
{
    /// <summary>Учитель вместе с группами, которые он ведёт.</summary>
    Task<Teacher?> GetWithGroupsAsync(Guid id, CancellationToken cancellationToken = default);
}
