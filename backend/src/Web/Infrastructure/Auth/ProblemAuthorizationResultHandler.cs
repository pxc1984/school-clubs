using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using SchoolClubs.Application.Common.Exceptions;

namespace SchoolClubs.Web.Infrastructure.Auth;

/// <summary>
/// Отказ из-за непринятого согласия отдаёт 403 с <c>code = consent_required</c>, чтобы фронт увёл на /consent.
/// </summary>
public class ProblemAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var consentMissing = authorizeResult.Forbidden
                             && authorizeResult.AuthorizationFailure?.FailedRequirements.OfType<ConsentRequirement>()
                                 .Any() == true;
        if (!consentMissing)
        {
            await _default.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Consent required",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            Extensions = { ["code"] = ForbiddenAccessException.ConsentRequired },
        });
    }
}
