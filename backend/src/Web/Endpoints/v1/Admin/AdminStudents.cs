using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Services.Admin;

namespace SchoolClubs.Web.Endpoints.v1.Admin;

public static class AdminStudents
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Search);
        group.MapGet("/{id:guid}", Get);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);
        group.MapPost("/{id:guid}/merge-into/{targetId:guid}", Merge);
        group.MapPost("/parse-names", ParseNames);
    }

    [EndpointSummary("Search students")]
    public static async Task<IReadOnlyList<StudentListItemDto>> Search(IAdminStudentService service,
        string? q,
        Guid? groupId,
        CancellationToken cancellationToken)
    {
        return await service.SearchAsync(q, groupId, cancellationToken);
    }

    [EndpointSummary("Get student with groups and parents")]
    public static async Task<StudentDetailsDto> Get(IAdminStudentService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.GetAsync(id, cancellationToken);
    }

    [EndpointSummary("Create student")]
    public static async Task<Created<StudentDetailsDto>> Create(IAdminStudentService service,
        CreateStudentRequest request,
        CancellationToken cancellationToken)
    {
        var student = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/v1/admin/students/{student.Id}", student);
    }

    [EndpointSummary("Edit student name and grade")]
    public static async Task<StudentDetailsDto> Update(IAdminStudentService service,
        Guid id,
        UpdateStudentRequest request,
        CancellationToken cancellationToken)
    {
        return await service.UpdateAsync(id, request, cancellationToken);
    }

    [EndpointSummary("Delete student without marks")]
    public static async Task<NoContent> Delete(IAdminStudentService service, Guid id,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Merge duplicate student into another one")]
    public static async Task<MergeStudentsResultDto> Merge(IAdminStudentService service,
        Guid id,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        return await service.MergeAsync(id, targetId, cancellationToken);
    }

    [EndpointSummary("Parse a pasted list of student names with duplicate hints")]
    public static async Task<IReadOnlyList<ParsedNameDto>> ParseNames(IAdminStudentService service,
        ParseNamesRequest request,
        CancellationToken cancellationToken)
    {
        return await service.ParseNamesAsync(request, cancellationToken);
    }
}
