using SchoolClubs.Domain.Entities.Access;

namespace SchoolClubs.Domain.Abstractions;

public interface IAccessRequestRepository : IRepository<AccessRequest>
{
    /// <summary>Запросы родителя, новые сверху.</summary>
    Task<IReadOnlyList<AccessRequest>> ListByParentAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Ожидающие — старые сверху (очередь); обработанные — недавно решённые сверху.</summary>
    Task<IReadOnlyList<AccessRequest>> ListByStatusAsync(AccessRequestStatus? status,
        int limit,
        CancellationToken cancellationToken = default);

    Task<int> CountPendingAsync(Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>Есть ли у родителя ожидающий запрос на того же ребёнка в той же группе.</summary>
    Task<bool> HasPendingDuplicateAsync(Guid userId,
        Guid groupId,
        string childLastName,
        string childFirstName,
        CancellationToken cancellationToken = default);
}
