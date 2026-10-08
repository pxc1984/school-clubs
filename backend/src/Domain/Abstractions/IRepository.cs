namespace SchoolClubs.Domain.Abstractions;

/// <summary>
/// Общий контракт репозитория в духе Clean Architecture:
/// абстракция живёт в Domain, реализация (EF Core) — в Infrastructure.
/// </summary>
public interface IRepository<TEntity>
    where TEntity : class
{
    /// <summary>Запрос для чтения с возможностью дальнейшего уточнения (Include, Where, пагинация).</summary>
    IQueryable<TEntity> Query();

    /// <summary>Поиск сущности по первичному ключу.</summary>
    Task<TEntity?> FindByIdAsync(params object[] keys);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
