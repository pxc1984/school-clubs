using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Common.Exceptions;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Web.Infrastructure.Auth;

namespace SchoolClubs.Web.Endpoints.v1.Auth;

public static class Auth
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        // Единственный параметр HttpContext — без приведения метод приняли бы за RequestDelegate.
        group.MapPost("/logout", (Delegate) Logout);
        group.MapGet("/me", Me).RequireAuthorization(Policies.Session);

        group.MapGet("/providers", Providers);
        group.MapGet("/external/{provider}", ExternalChallenge);
        group.MapGet("/external/{provider}/complete", ExternalComplete);
    }

    [EndpointSummary("Register a parent with email and password")]
    public static async Task<NoContent> Register(IAuthService service,
        HttpContext httpContext,
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var session = await service.RegisterAsync(request, cancellationToken);
        await httpContext.SignInSessionAsync(session);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Log in with email and password")]
    public static async Task<NoContent> Login(IAuthService service,
        HttpContext httpContext,
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var session = await service.LoginAsync(request, cancellationToken);
        await httpContext.SignInSessionAsync(session);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Log out")]
    public static async Task<NoContent> Logout(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(AuthSchemes.Session);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Current user and roles")]
    public static async Task<MeDto> Me(IAuthService service, CancellationToken cancellationToken)
    {
        return await service.GetMeAsync(cancellationToken);
    }

    [EndpointSummary("Enabled external login providers")]
    public static IReadOnlyList<ExternalProviderDto> Providers(OidcProviderRegistry registry)
    {
        return registry.List();
    }

    [EndpointSummary("Start login with an external provider")]
    public static Results<ChallengeHttpResult, NotFound> ExternalChallenge(OidcProviderRegistry registry,
        string provider,
        string? returnUrl)
    {
        if (!registry.TryGet(provider, out _))
            return TypedResults.NotFound();

        var name = provider.ToLowerInvariant();
        var complete = $"/api/v1/auth/external/{name}/complete?returnUrl={Uri.EscapeDataString(LocalUrl.OrDefault(returnUrl))}";
        return TypedResults.Challenge(new AuthenticationProperties { RedirectUri = complete },
            [OidcProviderRegistry.SchemeOf(name)]);
    }

    /// <summary>Сюда провайдер возвращает пользователя после callback: выпускаем сессию и уводим на фронт.</summary>
    [EndpointSummary("Finish login with an external provider")]
    public static async Task<RedirectHttpResult> ExternalComplete(IAuthService service,
        OidcProviderRegistry registry,
        HttpContext httpContext,
        string provider,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        var external = await httpContext.AuthenticateAsync(AuthSchemes.External);
        await httpContext.SignOutAsync(AuthSchemes.External);

        var name = provider.ToLowerInvariant();
        // Временную cookie должна была выпустить именно схема этого провайдера.
        if (!external.Succeeded
            || !registry.TryGet(name, out var options)
            || external.Properties?.Items.TryGetValue(".AuthScheme", out var scheme) != true
            || scheme != OidcProviderRegistry.SchemeOf(name)
            || OidcProviderRegistry.ToIdentity(name, options, external.Principal!) is not { } identity)
            return TypedResults.LocalRedirect("/login?error=external");

        SessionDto session;
        try
        {
            session = await service.ExternalLoginAsync(identity, cancellationToken);
        }
        catch (ForbiddenAccessException)
        {
            return TypedResults.LocalRedirect("/login?error=blocked");
        }

        await httpContext.SignInSessionAsync(session);
        var target = LocalUrl.OrDefault(returnUrl);
        return TypedResults.LocalRedirect(session.ConsentRequired
            ? $"/consent?returnUrl={Uri.EscapeDataString(target)}"
            : target);
    }
}
