using Microsoft.AspNetCore.Authorization;

namespace SchoolClubs.Web.Infrastructure.Auth;

/// <summary>Пользователь принял текущую версию политики ПДн (152-ФЗ).</summary>
public class ConsentRequirement : AuthorizationHandler<ConsentRequirement>, IAuthorizationRequirement
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ConsentRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && !context.User.HasClaim(c => c.Type == SessionClaims.ConsentRequired))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
