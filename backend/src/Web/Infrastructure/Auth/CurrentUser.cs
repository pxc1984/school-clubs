using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Web.Infrastructure.Auth;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get { return Principal?.GetUserId(); }
    }

    public bool IsAuthenticated
    {
        get { return Principal?.Identity?.IsAuthenticated == true; }
    }

    public bool IsServiceAdmin
    {
        get { return IsAuthenticated && Principal!.IsServiceAdmin(); }
    }

    public IReadOnlyCollection<UserRole> Roles
    {
        get
        {
            if (!IsAuthenticated)
                return [];

            return Principal!.FindAll(SessionClaims.Role)
                .Select(c => Enum.TryParse<UserRole>(c.Value, out var role) ? role : (UserRole?) null)
                .OfType<UserRole>()
                .ToList();
        }
    }

    public string? IpAddress
    {
        get { return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(); }
    }

    private System.Security.Claims.ClaimsPrincipal? Principal
    {
        get { return httpContextAccessor.HttpContext?.User; }
    }

    public bool IsInRole(UserRole role)
    {
        return Roles.Contains(role);
    }
}
