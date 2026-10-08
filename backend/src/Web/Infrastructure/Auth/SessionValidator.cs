using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SchoolClubs.Application.Services.Auth;

namespace SchoolClubs.Web.Infrastructure.Auth;

/// <summary>
/// Периодически сверяет cookie с БД: заблокированного или удалённого пользователя выкидывает,
/// при смене <c>SecurityStamp</c> (роли, сброс пароля) или статуса согласия перевыпускает клеймы.
/// </summary>
public static class SessionValidator
{
    public const string IntervalKey = "Auth:SessionValidationInterval";

    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal is null || principal.IsServiceAdmin())
            return;

        var services = context.HttpContext.RequestServices;
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        var interval = services.GetRequiredService<IConfiguration>().GetValue(IntervalKey, TimeSpan.FromMinutes(5));
        if (context.Properties.Items.TryGetValue(SessionPrincipal.ValidatedAtItem, out var raw)
            && DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var validatedAt)
            && now - validatedAt < interval)
            return;

        var session = principal.GetUserId() is { } userId
            ? await services.GetRequiredService<IAuthService>().RefreshSessionAsync(userId, context.HttpContext.RequestAborted)
            : null;
        if (session is null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(AuthSchemes.Session);
            return;
        }

        if (IsOutdated(principal, session))
            context.ReplacePrincipal(SessionPrincipal.Create(session));
        context.Properties.Items[SessionPrincipal.ValidatedAtItem] = now.ToString("O", CultureInfo.InvariantCulture);
        context.ShouldRenew = true;
    }

    private static bool IsOutdated(ClaimsPrincipal principal, SessionDto session)
    {
        return principal.FindFirstValue(SessionClaims.SecurityStamp) != session.SecurityStamp
               || principal.HasClaim(c => c.Type == SessionClaims.ConsentRequired) != session.ConsentRequired;
    }
}
