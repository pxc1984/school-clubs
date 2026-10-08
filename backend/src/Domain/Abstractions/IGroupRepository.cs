using Group = SchoolClubs.Domain.Entities.Gradebook.Group;

namespace SchoolClubs.Domain.Abstractions;

public interface IGroupRepository : IRepository<Group>
{
    /// <summary>Группа вместе с учителями и учениками.</summary>
    Task<Group?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Все группы с учителями и учениками, по названию.</summary>
    Task<IReadOnlyList<Group>> ListWithMembersAsync(bool includeArchived,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Group>> ListByTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Group>> ListByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>Группы, где есть хотя бы один из учеников, вместе с составом.</summary>
    Task<IReadOnlyList<Group>> ListWithStudentsByStudentsAsync(IReadOnlyCollection<Guid> studentIds,
        CancellationToken cancellationToken = default);

    Task<bool> HasLessonsAsync(Guid groupId, CancellationToken cancellationToken = default);
}
