namespace SchoolClubs.Domain.Abstractions;

/// <summary>Пример специфичного репозитория: generic-контракт + доменные запросы.</summary>
public interface ITodoItemRepository : IRepository<TodoItem>
{
    Task<IReadOnlyList<TodoItem>> ListAsync(bool? done, CancellationToken cancellationToken = default);
}
