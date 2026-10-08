using SchoolClubs.Web.Endpoints.v1.TodoItems;

namespace SchoolClubs.Web.Infrastructure;

public static class WebApplicationExtensions
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var v1 = app.MapGroup("/api/v1");

        TodoItems.Map(v1
            .MapGroup("/todo-items")
            .WithTags("TodoItems"));

        return app;
    }
}
