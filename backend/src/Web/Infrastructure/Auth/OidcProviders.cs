using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SchoolClubs.Application.Services.Auth;

namespace SchoolClubs.Web.Infrastructure.Auth;

/// <summary>
/// Один внешний OIDC-провайдер из секции <c>Auth:Oidc:&lt;name&gt;</c>. Новый провайдер (Google, VK ID, …)
/// подключается только конфигом; провайдер с пустым <see cref="ClientId"/> выключен.
/// </summary>
public class OidcProviderOptions
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Issuer с <c>/.well-known/openid-configuration</c>.</summary>
    public string Authority { get; set; } = string.Empty;

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string[] Scopes { get; set; } = ["openid", "profile", "email"];

    /// <summary>
    /// Провайдер выдаёт только подтверждённые адреса, даже без claim <c>email_verified</c>.
    /// Включать, только если это гарантировано: по email находится аккаунт, заведённый админом.
    /// </summary>
    public bool TrustEmail { get; set; }

    public bool IsEnabled
    {
        get { return !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(Authority); }
    }
}

public record ExternalProviderDto(string Name, string DisplayName);

/// <summary>Включённые OIDC-провайдеры; имя провайдера — ключ секции в нижнем регистре.</summary>
public class OidcProviderRegistry
{
    public const string SectionName = "Auth:Oidc";

    private readonly Dictionary<string, OidcProviderOptions> _providers;

    public OidcProviderRegistry(IConfiguration configuration)
    {
        _providers = configuration.GetSection(SectionName).GetChildren()
            .Select(section => (Name: section.Key.ToLowerInvariant(), Options: section.Get<OidcProviderOptions>()))
            .Where(p => p.Options?.IsEnabled == true)
            .ToDictionary(p => p.Name, p => p.Options!);
    }

    public IReadOnlyList<ExternalProviderDto> List()
    {
        return _providers
            .Select(p => new ExternalProviderDto(p.Key, string.IsNullOrEmpty(p.Value.DisplayName) ? p.Key : p.Value.DisplayName))
            .OrderBy(p => p.Name)
            .ToList();
    }

    public bool TryGet(string name, out OidcProviderOptions options)
    {
        return _providers.TryGetValue(name.ToLowerInvariant(), out options!);
    }

    public IEnumerable<KeyValuePair<string, OidcProviderOptions>> All
    {
        get { return _providers; }
    }

    public static string SchemeOf(string provider)
    {
        return AuthSchemes.OidcPrefix + provider.ToLowerInvariant();
    }

    public static string CallbackPathOf(string provider)
    {
        return $"/api/v1/auth/external/{provider.ToLowerInvariant()}/callback";
    }

    /// <summary>Стандартные OIDC-клеймы → провайдер-независимый <see cref="ExternalIdentity"/>.</summary>
    public static ExternalIdentity? ToIdentity(string provider, OidcProviderOptions options, ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
            return null;

        var emailVerified = options.TrustEmail
                            || string.Equals(principal.FindFirstValue("email_verified"), "true",
                                StringComparison.OrdinalIgnoreCase);
        return new ExternalIdentity(provider,
            subject,
            principal.FindFirstValue("email"),
            emailVerified,
            principal.FindFirstValue("given_name"),
            principal.FindFirstValue("family_name"),
            principal.FindFirstValue("middle_name"));
    }
}

public static class OidcAuthenticationBuilderExtensions
{
    /// <summary>Регистрирует по OIDC-схеме на каждый включённый провайдер.</summary>
    public static AuthenticationBuilder AddOidcProviders(this AuthenticationBuilder builder,
        OidcProviderRegistry registry)
    {
        foreach (var (name, provider) in registry.All)
        {
            builder.AddOpenIdConnect(OidcProviderRegistry.SchemeOf(name), provider.DisplayName, options =>
            {
                options.SignInScheme = AuthSchemes.External;
                options.Authority = provider.Authority;
                options.ClientId = provider.ClientId;
                options.ClientSecret = provider.ClientSecret;
                options.CallbackPath = OidcProviderRegistry.CallbackPathOf(name);

                options.ResponseType = OpenIdConnectResponseType.Code;
                // query, а не form_post: callback — обычный GET, и correlation-cookie с SameSite=Lax доходят
                // даже по http в локальной разработке.
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.UsePkce = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.MapInboundClaims = false;
                options.SaveTokens = false;
                options.Scope.Clear();
                foreach (var scope in provider.Scopes)
                    options.Scope.Add(scope);

                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                options.Events.OnRemoteFailure = context =>
                {
                    context.Response.Redirect("/login?error=external");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });
        }

        return builder;
    }
}
