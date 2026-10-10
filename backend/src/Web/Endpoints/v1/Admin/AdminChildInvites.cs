using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Services.ChildInvites;

namespace SchoolClubs.Web.Endpoints.v1.Admin;

public static class AdminChildInvites
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", List);
        group.MapPost("/", Create);
        group.MapDelete("/{inviteId:guid}", Revoke);
    }

    [EndpointSummary("Active invite links for a student")]
    public static async Task<IReadOnlyList<ChildInviteDto>> List(IAdminChildInviteService service,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        return await service.ListAsync(studentId, cancellationToken);
    }

    [EndpointSummary("Create a one-time invite link that links the student to whoever opens it")]
    public static async Task<Created<ChildInviteDto>> Create(IAdminChildInviteService service,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var invite = await service.CreateAsync(studentId, cancellationToken);
        return TypedResults.Created($"/api/v1/admin/students/{studentId}/invites", invite);
    }

    [EndpointSummary("Revoke an unused invite link")]
    public static async Task<NoContent> Revoke(IAdminChildInviteService service,
        Guid studentId,
        Guid inviteId,
        CancellationToken cancellationToken)
    {
        await service.RevokeAsync(studentId, inviteId, cancellationToken);
        return TypedResults.NoContent();
    }
}
