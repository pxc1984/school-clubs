using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class TodoItemRepository(DbContext dbContext) : Repository<TodoItem>(dbContext), ITodoItemRepository
{
    public async Task<IReadOnlyList<TodoItem>> ListAsync(bool? done, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();
        if (done is not null) query = query.Where(t => t.IsDone == done);

        return await query
            .OrderBy(t => t.IsDone)
            .ThenByDescending(t => t.Created)
            .ToListAsync(cancellationToken);
    }
}
