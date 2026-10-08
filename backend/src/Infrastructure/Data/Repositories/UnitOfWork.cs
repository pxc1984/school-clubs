using SchoolClubs.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace SchoolClubs.Infrastructure.Data.Repositories;

/// <summary>Реализация <see cref="IUnitOfWork"/> поверх <see cref="ApplicationDbContext"/>.</summary>
public sealed class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return new EfTransaction(await dbContext.Database.BeginTransactionAsync(cancellationToken));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public void ClearChangeTracker()
    {
        dbContext.ChangeTracker.Clear();
    }

    private sealed class EfTransaction(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction) : ITransaction
    {
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await transaction.RollbackAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync();
        }
    }
}