using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Application.Services.ChildInvites;
using SchoolClubs.Web.Infrastructure.Auth;

namespace SchoolClubs.Web.Endpoints.v1.ChildInvites;

public static class ChildInvites
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/{token}/accept", Accept).RequireRateLimiting(RateLimits.ChildInviteAccept);
    }

    /// <remarks>Роль Parent может появиться только что — перевыпускаем сессию, не дожидаясь сверки с БД.</remarks>
    [EndpointSummary("Link the child from an admin's invite link to the current user")]
    public static async Task<AcceptedChildInviteDto> Accept(IChildInviteService service,
        IAuthService authService,
        HttpContext httpContext,
        string token,
        CancellationToken cancellationToken)
    {
        var accepted = await service.AcceptAsync(token, cancellationToken);
        if (httpContext.User.GetUserId() is { } userId
            && await authService.RefreshSessionAsync(userId, cancellationToken) is { } session)
            await httpContext.SignInSessionAsync(session);
        return accepted;
    }
}
