using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Services.TodoItems;

namespace SchoolClubs.Web.Endpoints.v1.TodoItems;

public static class TodoItems
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", List);
        group.MapGet("/{id:guid}", Get);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);
    }

    [EndpointSummary("List todo items")]
    public static async Task<IReadOnlyList<TodoItemDto>> List(ITodoItemService service,
        bool? done,
        CancellationToken cancellationToken)
    {
        return await service.ListAsync(done, cancellationToken);
    }

    [EndpointSummary("Get todo item by id")]
    public static async Task<TodoItemDto> Get(ITodoItemService service, Guid id, CancellationToken cancellationToken)
    {
        return await service.GetAsync(id, cancellationToken);
    }

    [EndpointSummary("Create todo item")]
    public static async Task<Created<TodoItemDto>> Create(ITodoItemService service,
        CreateTodoItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/v1/todo-items/{item.Id}", item);
    }

    [EndpointSummary("Update todo item")]
    public static async Task<TodoItemDto> Update(ITodoItemService service,
        Guid id,
        UpdateTodoItemRequest request,
        CancellationToken cancellationToken)
    {
        return await service.UpdateAsync(id, request, cancellationToken);
    }

    [EndpointSummary("Delete todo item")]
    public static async Task<NoContent> Delete(ITodoItemService service, Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }
}
