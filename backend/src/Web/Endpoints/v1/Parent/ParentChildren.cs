using SchoolClubs.Application.Services.ParentDashboard;

namespace SchoolClubs.Web.Endpoints.v1.Parent;

public static class ParentChildren
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", List);
        group.MapGet("/{studentId:guid}/groups/{groupId:guid}/summary", Summary);
        group.MapGet("/{studentId:guid}/groups/{groupId:guid}/lessons", Lessons);
    }

    [EndpointSummary("My children with a short all-time summary per group")]
    public static async Task<IReadOnlyList<ChildDto>> List(IParentDashboardService service,
        CancellationToken cancellationToken)
    {
        return await service.ListChildrenAsync(cancellationToken);
    }

    [EndpointSummary("Child's summary in a group: totals, position bucket, chart series")]
    [EndpointDescription("Only published gradebooks count. Period bounds are inclusive; omit both for all time.")]
    public static async Task<ChildSummaryDto> Summary(IParentDashboardService service,
        Guid studentId,
        Guid groupId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        return await service.GetSummaryAsync(studentId, groupId, from, to, cancellationToken);
    }

    [EndpointSummary("Feed of published lessons with the child's marks and per-task group aggregates")]
    public static async Task<IReadOnlyList<ChildLessonDto>> Lessons(IParentDashboardService service,
        Guid studentId,
        Guid groupId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        return await service.ListLessonsAsync(studentId, groupId, from, to, cancellationToken);
    }
}
