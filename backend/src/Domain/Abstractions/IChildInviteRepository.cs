using SchoolClubs.Domain.Entities.Access;

namespace SchoolClubs.Domain.Abstractions;

public interface IChildInviteRepository : IRepository<ChildInvite>
{
    Task<ChildInvite?> FindByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Неиспользованные, неотозванные и неистёкшие приглашения ученика, новые сверху.</summary>
    Task<IReadOnlyList<ChildInvite>> ListActiveByStudentAsync(Guid studentId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
