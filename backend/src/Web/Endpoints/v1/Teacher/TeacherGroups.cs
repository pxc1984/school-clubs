using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Services.Admin;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Application.Services.GradebookImport;
using SchoolClubs.Application.Services.Lessons;
using SchoolClubs.Application.Services.TeacherGroups;

namespace SchoolClubs.Web.Endpoints.v1.Teacher;

public static class TeacherGroups
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", List);
        group.MapGet("/{id:guid}", Get);
        group.MapGet("/{id:guid}/summary", Summary);
        group.MapGet("/{id:guid}/lessons", ListLessons);
        group.MapPost("/{id:guid}/lessons", CreateLesson);
        group.MapPost("/{id:guid}/students", AddStudent);
        group.MapDelete("/{id:guid}/students/{studentId:guid}", RemoveStudent);
        group.MapGet("/{id:guid}/gradebook-template", Template);
    }

    [EndpointSummary("My groups")]
    public static async Task<IReadOnlyList<TeacherGroupCardDto>> List(ITeacherGroupService service,
        CancellationToken cancellationToken)
    {
        return await service.ListAsync(cancellationToken);
    }

    [EndpointSummary("Group with students and lessons")]
    public static async Task<TeacherGroupDto> Get(ITeacherGroupService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.GetAsync(id, cancellationToken);
    }

    [EndpointSummary("Students × lessons matrix of solved tasks")]
    public static async Task<GroupMatrixDto> Summary(ITeacherGroupService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.GetSummaryAsync(id, cancellationToken);
    }

    [EndpointSummary("Lessons of the group")]
    public static async Task<IReadOnlyList<LessonSummaryDto>> ListLessons(ITeacherGroupService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.ListLessonsAsync(id, cancellationToken);
    }

    [EndpointSummary("Create lesson with N tasks")]
    public static async Task<Created<LessonDto>> CreateLesson(ITeacherGroupService service,
        Guid id,
        CreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var lesson = await service.CreateLessonAsync(id, request, cancellationToken);
        return TypedResults.Created($"/api/v1/teacher/lessons/{lesson.Id}", lesson);
    }

    [EndpointSummary("Add new student to the group")]
    public static async Task<Created<StudentDto>> AddStudent(ITeacherGroupService service,
        Guid id,
        NewStudentRequest request,
        CancellationToken cancellationToken)
    {
        var student = await service.AddStudentAsync(id, request, cancellationToken);
        return TypedResults.Created($"/api/v1/teacher/groups/{id}", student);
    }

    [EndpointSummary("Remove student from the group (marks are kept)")]
    public static async Task<NoContent> RemoveStudent(ITeacherGroupService service,
        Guid id,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        await service.RemoveStudentAsync(id, studentId, cancellationToken);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Blank XLSX template of the group with N tasks")]
    public static async Task<FileContentHttpResult> Template(IGradebookImportService service,
        Guid id,
        int? tasks,
        CancellationToken cancellationToken)
    {
        var file = await service.GetGroupTemplateAsync(id, tasks ?? 1, cancellationToken);
        return TypedResults.File(file.Content, file.ContentType, file.FileName);
    }
}
