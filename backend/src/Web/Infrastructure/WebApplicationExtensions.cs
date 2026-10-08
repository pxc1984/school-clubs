using SchoolClubs.Web.Endpoints.v1.Account;
using SchoolClubs.Web.Endpoints.v1.Admin;
using SchoolClubs.Web.Endpoints.v1.TodoItems;
using SchoolClubs.Web.Infrastructure.Auth;
using AuthEndpoints = SchoolClubs.Web.Endpoints.v1.Auth.Auth;

namespace SchoolClubs.Web.Infrastructure;

public static class WebApplicationExtensions
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var v1 = app.MapGroup("/api/v1");

        TodoItems.Map(v1
            .MapGroup("/todo-items")
            .WithTags("TodoItems"));

        AuthEndpoints.Map(v1
            .MapGroup("/auth")
            .WithTags("Auth"));

        Account.Map(v1
            .MapGroup("/account")
            .RequireAuthorization(Policies.User)
            .WithTags("Account"));

        // Вход сервисного админа анонимный; остальные /admin/* — под политикой Admin (см. Policies).
        AdminAuth.Map(v1
            .MapGroup("/admin")
            .WithTags("Admin"));

        return app;
    }
}
