using SchoolClubs.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace SchoolClubs.Infrastructure.Data.Repositories;

/// <summary>
/// Базовая реализация репозитория поверх EF Core.
/// Принимает базовый <see cref="DbContext"/>, который в DI маппится на <see cref="ApplicationDbContext"/>.
/// Сохранение изменений выполняется явно через <see cref="SaveChangesAsync"/>.
/// </summary>
public class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    protected readonly DbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    public Repository(DbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
    }

    public IQueryable<TEntity> Query()
    {
        return DbSet.AsQueryable();
    }

    public async Task<TEntity?> FindByIdAsync(params object[] keys)
    {
        return await DbSet.FindAsync(keys);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.SaveChangesAsync(cancellationToken);
    }
}