namespace SchoolClubs.Application.Services.TodoItems;

public interface ITodoItemService
{
    Task<IReadOnlyList<TodoItemDto>> ListAsync(bool? done, CancellationToken cancellationToken = default);
    Task<TodoItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TodoItemDto> CreateAsync(CreateTodoItemRequest request, CancellationToken cancellationToken = default);
    Task<TodoItemDto> UpdateAsync(Guid id, UpdateTodoItemRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
