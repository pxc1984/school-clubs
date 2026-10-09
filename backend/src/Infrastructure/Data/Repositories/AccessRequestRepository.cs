using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Access;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class AccessRequestRepository(DbContext dbContext)
    : Repository<AccessRequest>(dbContext), IAccessRequestRepository
{
    public async Task<IReadOnlyList<AccessRequest>> ListByParentAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Created)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccessRequest>> ListByStatusAsync(AccessRequestStatus? status,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var requests = DbSet.AsQueryable();
        if (status is { } s)
            requests = requests.Where(r => r.Status == s);

        requests = status == AccessRequestStatus.Pending
            ? requests.OrderBy(r => r.Created)
            : requests.OrderBy(r => r.Status != AccessRequestStatus.Pending)
                .ThenByDescending(r => r.DecidedAt ?? r.Created);

        return await requests.Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<int> CountPendingAsync(Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var pending = DbSet.Where(r => r.Status == AccessRequestStatus.Pending);
        if (userId is { } id)
            pending = pending.Where(r => r.UserId == id);
        return await pending.CountAsync(cancellationToken);
    }

    public async Task<bool> HasPendingDuplicateAsync(Guid userId,
        Guid groupId,
        string childLastName,
        string childFirstName,
        CancellationToken cancellationToken = default)
    {
        // Ожидающих у родителя не больше пяти — сравниваем в памяти с той же нормализацией, что в домене.
        var pending = await DbSet
            .Where(r => r.UserId == userId && r.GroupId == groupId && r.Status == AccessRequestStatus.Pending)
            .ToListAsync(cancellationToken);
        return pending.Any(r => r.IsSameChild(groupId, childLastName, childFirstName));
    }
}
