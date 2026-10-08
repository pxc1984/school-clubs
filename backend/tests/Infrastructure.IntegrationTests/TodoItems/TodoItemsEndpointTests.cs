using System.Net;
using System.Net.Http.Json;
using SchoolClubs.Application.Services.TodoItems;

namespace SchoolClubs.Infrastructure.IntegrationTests.TodoItems;

public class TodoItemsEndpointTests : TestBase
{
    private const string Url = "/api/v1/todo-items";

    [Test]
    public async Task Create_ThenList_ReturnsItem()
    {
        var create = await Client.PostAsJsonAsync(Url, new CreateTodoItemRequest("Write tests"));
        create.StatusCode.ShouldBe(HttpStatusCode.Created);

        var items = await Client.GetFromJsonAsync<List<TodoItemDto>>(Url);

        items.ShouldNotBeNull();
        items.ShouldHaveSingleItem().Title.ShouldBe("Write tests");
    }

    [Test]
    public async Task Create_EmptyTitle_Returns400()
    {
        var response = await Client.PostAsJsonAsync(Url, new CreateTodoItemRequest(""));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("\"Title\"");
    }

    [Test]
    public async Task Get_Missing_Returns404()
    {
        var response = await Client.GetAsync($"{Url}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Update_TogglesDone()
    {
        var created = await (await Client.PostAsJsonAsync(Url, new CreateTodoItemRequest("Toggle me")))
            .Content.ReadFromJsonAsync<TodoItemDto>();

        var response = await Client.PutAsJsonAsync($"{Url}/{created!.Id}", new UpdateTodoItemRequest("Toggle me", true));
        var updated = await response.Content.ReadFromJsonAsync<TodoItemDto>();

        updated!.IsDone.ShouldBeTrue();
    }
}
