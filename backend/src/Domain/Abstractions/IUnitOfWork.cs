namespace SchoolClubs.Domain.Abstractions;

/// <summary>
/// Граница транзакции. Реализация (EF Core) живёт в Infrastructure.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Единица работы: группирует репозитории в общую транзакцию.
/// Реализация (EF Core) живёт в Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Очищает трекер изменений контекста (для batch-процессов).</summary>
    void ClearChangeTracker();
}
