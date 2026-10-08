using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Services.Account;
using SchoolClubs.Web.Infrastructure.Auth;

namespace SchoolClubs.Web.Endpoints.v1.Account;

public static class Account
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Get).RequireAuthorization(Policies.Consent);
        // Единственный эндпоинт (кроме /auth/*), доступный до принятия согласия.
        group.MapPost("/consent", AcceptConsent);
    }

    [EndpointSummary("Current user's profile and login methods")]
    public static async Task<AccountDto> Get(IAccountService service, CancellationToken cancellationToken)
    {
        return await service.GetAsync(cancellationToken);
    }

    [EndpointSummary("Accept the current personal data policy")]
    public static async Task<NoContent> AcceptConsent(IAccountService service,
        HttpContext httpContext,
        AcceptConsentRequest request,
        CancellationToken cancellationToken)
    {
        var session = await service.AcceptConsentAsync(request, cancellationToken);
        await httpContext.SignInSessionAsync(session);
        return TypedResults.NoContent();
    }
}
