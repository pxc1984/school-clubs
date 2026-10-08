using FluentValidation;
using SchoolClubs.Domain.Abstractions;
using ValidationException = SchoolClubs.Application.Common.Exceptions.ValidationException;

namespace SchoolClubs.Application.Services.TodoItems;

public class TodoItemService(
    ITodoItemRepository todoItems,
    IValidator<CreateTodoItemRequest> createValidator,
    IValidator<UpdateTodoItemRequest> updateValidator) : ITodoItemService
{
    public async Task<IReadOnlyList<TodoItemDto>> ListAsync(bool? done, CancellationToken cancellationToken = default)
    {
        var items = await todoItems.ListAsync(done, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<TodoItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ToDto(await FindAsync(id));
    }

    public async Task<TodoItemDto> CreateAsync(CreateTodoItemRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(createValidator, request, cancellationToken);

        var item = new TodoItem { Title = request.Title.Trim() };
        await todoItems.AddAsync(item, cancellationToken);
        await todoItems.SaveChangesAsync(cancellationToken);

        return ToDto(item);
    }

    public async Task<TodoItemDto> UpdateAsync(Guid id,
        UpdateTodoItemRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(updateValidator, request, cancellationToken);

        var item = await FindAsync(id);
        item.Title = request.Title.Trim();
        item.IsDone = request.IsDone;
        await todoItems.SaveChangesAsync(cancellationToken);

        return ToDto(item);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await FindAsync(id);
        todoItems.Remove(item);
        await todoItems.SaveChangesAsync(cancellationToken);
    }

    private async Task<TodoItem> FindAsync(Guid id)
    {
        var item = await todoItems.FindByIdAsync(id);
        Guard.Against.NotFound(id, item);
        return item;
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);
    }

    private static TodoItemDto ToDto(TodoItem item)
    {
        return new TodoItemDto(item.Id, item.Title, item.IsDone, item.Created);
    }
}
