using SchoolClubs.Application.Services.AccessRequests;
using SchoolClubs.Domain.Entities.Access;

namespace SchoolClubs.Web.Endpoints.v1.Admin;

public static class AdminAccessRequests
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", List);
        group.MapGet("/pending-count", PendingCount);
        group.MapGet("/{id:guid}", Get);
        group.MapPost("/{id:guid}/approve", Approve);
        group.MapPost("/{id:guid}/reject", Reject);
    }

    [EndpointSummary("Access request queue (pending: oldest first)")]
    public static async Task<IReadOnlyList<AdminAccessRequestDto>> List(IAdminAccessRequestService service,
        AccessRequestStatus? status,
        CancellationToken cancellationToken)
    {
        return await service.ListAsync(status, cancellationToken);
    }

    [EndpointSummary("Number of pending requests for the admin header")]
    public static async Task<PendingCountDto> PendingCount(IAdminAccessRequestService service,
        CancellationToken cancellationToken)
    {
        return await service.CountPendingAsync(cancellationToken);
    }

    [EndpointSummary("Access request card with parent and similar students")]
    public static async Task<AdminAccessRequestDto> Get(IAdminAccessRequestService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.GetAsync(id, cancellationToken);
    }

    [EndpointSummary("Approve with an existing or a new student")]
    public static async Task<AdminAccessRequestDto> Approve(IAdminAccessRequestService service,
        Guid id,
        ApproveAccessRequestRequest request,
        CancellationToken cancellationToken)
    {
        return await service.ApproveAsync(id, request, cancellationToken);
    }

    [EndpointSummary("Reject with a reason the parent will see")]
    public static async Task<AdminAccessRequestDto> Reject(IAdminAccessRequestService service,
        Guid id,
        RejectAccessRequestRequest request,
        CancellationToken cancellationToken)
    {
        return await service.RejectAsync(id, request, cancellationToken);
    }
}
