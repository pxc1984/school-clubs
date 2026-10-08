using Group = SchoolClubs.Domain.Entities.Gradebook.Group;

namespace SchoolClubs.Domain.Abstractions;

public interface IGroupRepository : IRepository<Group>
{
    /// <summary>Группа вместе с учителями и учениками.</summary>
    Task<Group?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Group>> ListByTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Group>> ListByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
}
