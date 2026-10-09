using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Services.Account;
using SchoolClubs.Web.Infrastructure.Auth;

namespace SchoolClubs.Web.Endpoints.v1.Account;

public static class Account
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Get).RequireAuthorization(Policies.Consent);
        group.MapPut("/", Update).RequireAuthorization(Policies.Consent);
        group.MapPost("/password", ChangePassword).RequireAuthorization(Policies.Consent);
        group.MapDelete("/logins/{provider}", UnlinkLogin).RequireAuthorization(Policies.Consent);
        group.MapPut("/username", ChangeUsername).RequireAuthorization(Policies.Consent);
        // Единственный эндпоинт (кроме /auth/*), доступный до принятия согласия.
        group.MapPost("/consent", AcceptConsent);
    }

    [EndpointSummary("Current user's profile and login methods")]
    public static async Task<AccountDto> Get(IAccountService service, CancellationToken cancellationToken)
    {
        return await service.GetAsync(cancellationToken);
    }

    [EndpointSummary("Update the current user's name and phone")]
    public static async Task<AccountDto> Update(IAccountService service,
        UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        return await service.UpdateAsync(request, cancellationToken);
    }

    [EndpointSummary("Change the password, or set the first one for an external-only account")]
    public static async Task<NoContent> ChangePassword(IAccountService service,
        HttpContext httpContext,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var session = await service.ChangePasswordAsync(request, cancellationToken);
        await httpContext.SignInSessionAsync(session);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Unlink an external login provider")]
    public static async Task<NoContent> UnlinkLogin(IAccountService service,
        string provider,
        CancellationToken cancellationToken)
    {
        await service.UnlinkLoginAsync(provider, cancellationToken);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Set or clear the current user's username")]
    public static async Task<AccountDto> ChangeUsername(IAccountService service,
        ChangeUsernameRequest request,
        CancellationToken cancellationToken)
    {
        return await service.ChangeUsernameAsync(request, cancellationToken);
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
