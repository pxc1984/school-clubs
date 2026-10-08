using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Services.Gradebook;
using SchoolClubs.Application.Services.Lessons;

namespace SchoolClubs.Web.Endpoints.v1.Teacher;

public static class TeacherLessons
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", Get);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        group.MapPost("/{id:guid}/tasks", AddTask);
        group.MapPut("/{id:guid}/tasks/{taskId:guid}", RenameTask);
        group.MapDelete("/{id:guid}/tasks/{taskId:guid}", DeleteTask);

        group.MapGet("/{id:guid}/gradebook", GetGradebook);
        group.MapPut("/{id:guid}/gradebook", SaveGradebook);
        group.MapPost("/{id:guid}/gradebook/publish", Publish);
        group.MapPost("/{id:guid}/gradebook/unpublish", Unpublish);
    }

    [EndpointSummary("Lesson with tasks")]
    public static async Task<LessonDto> Get(ILessonService service, Guid id, CancellationToken cancellationToken)
    {
        return await service.GetAsync(id, cancellationToken);
    }

    [EndpointSummary("Change lesson date, time and topic")]
    public static async Task<LessonDto> Update(ILessonService service,
        Guid id,
        UpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        return await service.UpdateAsync(id, request, cancellationToken);
    }

    [EndpointSummary("Delete lesson without marks")]
    public static async Task<NoContent> Delete(ILessonService service, Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Add task")]
    public static async Task<LessonDto> AddTask(ILessonService service,
        Guid id,
        TaskTitleRequest request,
        CancellationToken cancellationToken)
    {
        return await service.AddTaskAsync(id, request, cancellationToken);
    }

    [EndpointSummary("Rename task")]
    public static async Task<LessonDto> RenameTask(ILessonService service,
        Guid id,
        Guid taskId,
        TaskTitleRequest request,
        CancellationToken cancellationToken)
    {
        return await service.RenameTaskAsync(id, taskId, request, cancellationToken);
    }

    [EndpointSummary("Delete task with its marks")]
    public static async Task<LessonDto> DeleteTask(ILessonService service,
        Guid id,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return await service.DeleteTaskAsync(id, taskId, cancellationToken);
    }

    [EndpointSummary("Gradebook grid of the lesson")]
    public static async Task<GradebookDto> GetGradebook(IGradebookService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.GetAsync(id, cancellationToken);
    }

    [EndpointSummary("Save changed gradebook cells")]
    public static async Task<GradebookDto> SaveGradebook(IGradebookService service,
        Guid id,
        SaveGradebookRequest request,
        CancellationToken cancellationToken)
    {
        return await service.SaveAsync(id, request, cancellationToken);
    }

    [EndpointSummary("Publish gradebook to parents")]
    public static async Task<GradebookDto> Publish(IGradebookService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.PublishAsync(id, cancellationToken);
    }

    [EndpointSummary("Unpublish gradebook")]
    public static async Task<GradebookDto> Unpublish(IGradebookService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.UnpublishAsync(id, cancellationToken);
    }
}
