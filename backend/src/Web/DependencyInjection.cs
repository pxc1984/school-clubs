using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Domain.Entities.Users;
using SchoolClubs.Infrastructure.Data;
using SchoolClubs.Web.Infrastructure.Auth;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();

        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddOpenApi();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();

        // Наружу торчит только traefik: доверяем его X-Forwarded-* (реальный IP для rate limit и согласия на ПДн).
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // Ключи шифруют cookie сессий: без постоянного хранилища каждый рестарт контейнера разлогинивает всех.
        var dataProtection = builder.Services.AddDataProtection().SetApplicationName("SchoolClubs");
        var keysPath = builder.Configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        builder.AddAuth();
    }

    private static void AddAuth(this IHostApplicationBuilder builder)
    {
        var oidcProviders = new OidcProviderRegistry(builder.Configuration);
        builder.Services.AddSingleton(oidcProviders);

        builder.Services
            .AddAuthentication(AuthSchemes.Session)
            .AddCookie(AuthSchemes.Session, options =>
            {
                options.Cookie.Name = "sc_session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = SessionPrincipal.UserLifetime;
                options.SlidingExpiration = true;
                options.Events = new CookieAuthenticationEvents
                {
                    // API: вместо редиректа на страницу входа — коды ответа.
                    OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    },
                    OnValidatePrincipal = SessionValidator.ValidateAsync,
                };
            })
            .AddCookie(AuthSchemes.External, options =>
            {
                options.Cookie.Name = "sc_external";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddOidcProviders(oidcProviders);

        builder.Services.AddSingleton<IAuthorizationHandler, ConsentRequirement>();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemAuthorizationResultHandler>();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Session, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(Policies.User, policy => policy.RequireClaim(SessionClaims.Subject))
            .AddPolicy(Policies.Consent, policy => policy.AddRequirements(new ConsentRequirement()))
            .AddPolicy(Policies.Parent, policy => RequireRole(policy, UserRole.Parent))
            .AddPolicy(Policies.Teacher, policy => RequireRole(policy, UserRole.Teacher))
            .AddPolicy(Policies.Admin, policy => RequireRole(policy, UserRole.Admin));

        var attemptsPerMinute = builder.Configuration.GetValue("Auth:ServiceAdminLoginAttemptsPerMinute", 5);
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimits.ServiceAdminLogin, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = attemptsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });
    }

    private static void RequireRole(AuthorizationPolicyBuilder policy, UserRole role)
    {
        policy
            .RequireAuthenticatedUser()
            .RequireRole(role.ToString())
            .AddRequirements(new ConsentRequirement());
    }
}
