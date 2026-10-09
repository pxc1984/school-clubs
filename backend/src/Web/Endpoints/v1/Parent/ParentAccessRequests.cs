using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Services.AccessRequests;

namespace SchoolClubs.Web.Endpoints.v1.Parent;

public static class ParentAccessRequests
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/groups", Groups);
        group.MapGet("/access-requests", List);
        group.MapPost("/access-requests", Create);
        group.MapDelete("/access-requests/{id:guid}", Cancel);
    }

    [EndpointSummary("Active groups for the access request form (without members)")]
    public static async Task<IReadOnlyList<ParentGroupDto>> Groups(IAccessRequestService service,
        CancellationToken cancellationToken)
    {
        return await service.ListGroupsAsync(cancellationToken);
    }

    [EndpointSummary("My access requests, newest first")]
    public static async Task<IReadOnlyList<AccessRequestDto>> List(IAccessRequestService service,
        CancellationToken cancellationToken)
    {
        return await service.ListMineAsync(cancellationToken);
    }

    [EndpointSummary("Request access to a child's marks")]
    public static async Task<Created<AccessRequestDto>> Create(IAccessRequestService service,
        CreateAccessRequestRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created("/api/v1/parent/access-requests", created);
    }

    [EndpointSummary("Cancel a pending access request")]
    public static async Task<AccessRequestDto> Cancel(IAccessRequestService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.CancelAsync(id, cancellationToken);
    }
}
