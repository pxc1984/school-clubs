namespace SchoolClubs.Application.Services.TodoItems;

public record TodoItemDto(Guid Id, string Title, bool IsDone, DateTimeOffset Created);

public record CreateTodoItemRequest(string Title);

public record UpdateTodoItemRequest(string Title, bool IsDone);
