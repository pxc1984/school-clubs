using SchoolClubs.Application.Services.Admin;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Web.Endpoints.v1.Admin;

public static class AdminUsers
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Search);
        group.MapGet("/{id:guid}", Get);
        group.MapPut("/{id:guid}/roles", SetRoles);
        group.MapPost("/{id:guid}/block", Block);
        group.MapPost("/{id:guid}/unblock", Unblock);
        group.MapPost("/{id:guid}/reset-password", ResetPassword);
    }

    public static void MapTeachers(RouteGroupBuilder group)
    {
        group.MapPost("/invite", InviteTeacher);
    }

    public static void MapParents(RouteGroupBuilder group)
    {
        group.MapPost("/{parentId:guid}/children/{studentId:guid}", LinkChild);
        group.MapDelete("/{parentId:guid}/children/{studentId:guid}", UnlinkChild);
    }

    [EndpointSummary("Search users by name or email")]
    public static async Task<IReadOnlyList<UserSummaryDto>> Search(IAdminUserService service,
        string? q,
        UserRole? role,
        CancellationToken cancellationToken)
    {
        return await service.SearchAsync(q, role, cancellationToken);
    }

    [EndpointSummary("Get user with children, groups and login methods")]
    public static async Task<UserDetailsDto> Get(IAdminUserService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.GetAsync(id, cancellationToken);
    }

    [EndpointSummary("Set user roles")]
    public static async Task<UserDetailsDto> SetRoles(IAdminUserService service,
        Guid id,
        SetRolesRequest request,
        CancellationToken cancellationToken)
    {
        return await service.SetRolesAsync(id, request, cancellationToken);
    }

    [EndpointSummary("Block user and end their sessions")]
    public static async Task<UserDetailsDto> Block(IAdminUserService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.BlockAsync(id, cancellationToken);
    }

    [EndpointSummary("Unblock user")]
    public static async Task<UserDetailsDto> Unblock(IAdminUserService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.UnblockAsync(id, cancellationToken);
    }

    [EndpointSummary("Reset password to a temporary one shown once")]
    public static async Task<TemporaryPasswordDto> ResetPassword(IAdminUserService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.ResetPasswordAsync(id, cancellationToken);
    }

    [EndpointSummary("Create teacher by email (or grant the role to an existing user)")]
    public static async Task<UserDetailsDto> InviteTeacher(IAdminUserService service,
        InviteTeacherRequest request,
        CancellationToken cancellationToken)
    {
        return await service.InviteTeacherAsync(request, cancellationToken);
    }

    [EndpointSummary("Link child to parent")]
    public static async Task<UserDetailsDto> LinkChild(IAdminUserService service,
        Guid parentId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        return await service.LinkChildAsync(parentId, studentId, cancellationToken);
    }

    [EndpointSummary("Unlink child from parent")]
    public static async Task<UserDetailsDto> UnlinkChild(IAdminUserService service,
        Guid parentId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        return await service.UnlinkChildAsync(parentId, studentId, cancellationToken);
    }
}
