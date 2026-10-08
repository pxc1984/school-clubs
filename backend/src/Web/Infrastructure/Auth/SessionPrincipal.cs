using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using SchoolClubs.Application.Services.Auth;

namespace SchoolClubs.Web.Infrastructure.Auth;

/// <summary>Перевод <see cref="SessionDto"/> в клеймы cookie и выпуск сессии.</summary>
public static class SessionPrincipal
{
    /// <summary>Когда сессию последний раз сверяли с БД (см. <see cref="SessionValidator"/>).</summary>
    public const string ValidatedAtItem = ".validated";

    public static readonly TimeSpan UserLifetime = TimeSpan.FromDays(14);
    public static readonly TimeSpan ServiceAdminLifetime = TimeSpan.FromHours(8);

    public static ClaimsPrincipal Create(SessionDto session)
    {
        var identity = new ClaimsIdentity(AuthSchemes.Session, SessionClaims.Subject, SessionClaims.Role);
        if (session.UserId is { } userId)
            identity.AddClaim(new Claim(SessionClaims.Subject, userId.ToString()));
        if (session.SecurityStamp is not null)
            identity.AddClaim(new Claim(SessionClaims.SecurityStamp, session.SecurityStamp));
        if (session.IsServiceAdmin)
            identity.AddClaim(new Claim(SessionClaims.ServiceAdmin, "true"));
        if (session.ConsentRequired)
            identity.AddClaim(new Claim(SessionClaims.ConsentRequired, "true"));
        foreach (var role in session.Roles)
            identity.AddClaim(new Claim(SessionClaims.Role, role.ToString()));

        return new ClaimsPrincipal(identity);
    }

    public static async Task SignInSessionAsync(this HttpContext httpContext, SessionDto session)
    {
        var now = httpContext.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow();
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc = now,
        };
        properties.Items[ValidatedAtItem] = now.ToString("O", CultureInfo.InvariantCulture);

        // Сервисному админу — жёсткие 8 часов без продления; пользователям — скользящие 14 дней.
        if (session.IsServiceAdmin)
        {
            properties.ExpiresUtc = now + ServiceAdminLifetime;
            properties.AllowRefresh = false;
        }

        await httpContext.SignInAsync(AuthSchemes.Session, Create(session), properties);
    }

    public static bool IsServiceAdmin(this ClaimsPrincipal principal)
    {
        return principal.HasClaim(SessionClaims.ServiceAdmin, "true");
    }

    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        return Guid.TryParse(principal.FindFirstValue(SessionClaims.Subject), out var id) ? id : null;
    }
}
