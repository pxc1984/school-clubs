using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Abstractions;

public interface IStudentRepository : IRepository<Student>
{
    Task<IReadOnlyList<Student>> ListByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
}
