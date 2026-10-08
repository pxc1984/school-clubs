using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Services.Admin;

namespace SchoolClubs.Web.Endpoints.v1.Admin;

public static class AdminGroups
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", List);
        group.MapGet("/{id:guid}", Get);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);
        group.MapPost("/{id:guid}/teachers/{teacherId:guid}", AddTeacher);
        group.MapDelete("/{id:guid}/teachers/{teacherId:guid}", RemoveTeacher);
        group.MapPost("/{id:guid}/students", AddStudents);
        group.MapDelete("/{id:guid}/students/{studentId:guid}", RemoveStudent);
    }

    [EndpointSummary("List groups")]
    public static async Task<IReadOnlyList<GroupSummaryDto>> List(IAdminGroupService service,
        bool? includeArchived,
        CancellationToken cancellationToken)
    {
        return await service.ListAsync(includeArchived ?? false, cancellationToken);
    }

    [EndpointSummary("Get group with teachers and students")]
    public static async Task<GroupDetailsDto> Get(IAdminGroupService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.GetAsync(id, cancellationToken);
    }

    [EndpointSummary("Create group")]
    public static async Task<Created<GroupDetailsDto>> Create(IAdminGroupService service,
        CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var group = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/v1/admin/groups/{group.Id}", group);
    }

    [EndpointSummary("Rename or archive group")]
    public static async Task<GroupDetailsDto> Update(IAdminGroupService service,
        Guid id,
        UpdateGroupRequest request,
        CancellationToken cancellationToken)
    {
        return await service.UpdateAsync(id, request, cancellationToken);
    }

    [EndpointSummary("Delete group without lessons")]
    public static async Task<NoContent> Delete(IAdminGroupService service, Guid id,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Assign teacher to group")]
    public static async Task<GroupDetailsDto> AddTeacher(IAdminGroupService service,
        Guid id,
        Guid teacherId,
        CancellationToken cancellationToken)
    {
        return await service.AddTeacherAsync(id, teacherId, cancellationToken);
    }

    [EndpointSummary("Remove teacher from group")]
    public static async Task<GroupDetailsDto> RemoveTeacher(IAdminGroupService service,
        Guid id,
        Guid teacherId,
        CancellationToken cancellationToken)
    {
        return await service.RemoveTeacherAsync(id, teacherId, cancellationToken);
    }

    [EndpointSummary("Add existing or new students to group")]
    public static async Task<GroupDetailsDto> AddStudents(IAdminGroupService service,
        Guid id,
        AddGroupStudentsRequest request,
        CancellationToken cancellationToken)
    {
        return await service.AddStudentsAsync(id, request, cancellationToken);
    }

    [EndpointSummary("Remove student from group (marks are kept)")]
    public static async Task<GroupDetailsDto> RemoveStudent(IAdminGroupService service,
        Guid id,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        return await service.RemoveStudentAsync(id, studentId, cancellationToken);
    }
}
