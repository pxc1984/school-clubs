using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Web.Infrastructure.Auth;

namespace SchoolClubs.Web.Endpoints.v1.Admin;

/// <summary>Сервисный вход по паролю из конфига: первичная настройка и аварийный доступ.</summary>
public static class AdminAuth
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/login", Login).RequireRateLimiting(RateLimits.ServiceAdminLogin);
        // Единственный параметр HttpContext — без приведения метод приняли бы за RequestDelegate.
        group.MapPost("/logout", (Delegate) Logout);
    }

    [EndpointSummary("Service admin login")]
    public static async Task<NoContent> Login(IAuthService service,
        HttpContext httpContext,
        ServiceAdminLoginRequest request)
    {
        var session = service.LoginServiceAdmin(request);
        await httpContext.SignInSessionAsync(session);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Service admin logout")]
    public static async Task<NoContent> Logout(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(AuthSchemes.Session);
        return TypedResults.NoContent();
    }
}
