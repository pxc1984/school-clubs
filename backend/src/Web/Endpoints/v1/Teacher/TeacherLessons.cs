using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Services.Gradebook;
using SchoolClubs.Application.Services.GradebookImport;
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
        group.MapPut("/{id:guid}/tasks/{taskId:guid}", UpdateTask);
        group.MapDelete("/{id:guid}/tasks/{taskId:guid}", DeleteTask);

        group.MapGet("/{id:guid}/gradebook", GetGradebook);
        group.MapPut("/{id:guid}/gradebook", SaveGradebook);
        group.MapPost("/{id:guid}/gradebook/publish", Publish);
        group.MapPost("/{id:guid}/gradebook/unpublish", Unpublish);

        group.MapGet("/{id:guid}/gradebook/template", Template);
        // Мультипарт с нашим CSRF-заголовком; встроенный antiforgery-токен SPA не использует.
        group.MapPost("/{id:guid}/gradebook/import", Import).DisableAntiforgery();
        group.MapPost("/{id:guid}/gradebook/import/{uploadId:guid}/apply", ApplyImport);
        group.MapGet("/{id:guid}/gradebook/imports", ListImports);
        group.MapGet("/{id:guid}/gradebook/imports/{uploadId:guid}/file", DownloadImport);
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
        TaskRequest request,
        CancellationToken cancellationToken)
    {
        return await service.AddTaskAsync(id, request, cancellationToken);
    }

    [EndpointSummary("Change task number and title")]
    public static async Task<LessonDto> UpdateTask(ILessonService service,
        Guid id,
        Guid taskId,
        TaskRequest request,
        CancellationToken cancellationToken)
    {
        return await service.UpdateTaskAsync(id, taskId, request, cancellationToken);
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

    [EndpointSummary("XLSX template of the lesson")]
    public static async Task<FileContentHttpResult> Template(IGradebookImportService service, Guid id,
        CancellationToken cancellationToken)
    {
        var file = await service.GetLessonTemplateAsync(id, cancellationToken);
        return TypedResults.File(file.Content, file.ContentType, file.FileName);
    }

    [EndpointSummary("Upload XLSX and preview the import (phase 1)")]
    public static async Task<ImportPreviewDto> Import(IGradebookImportService service,
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        return await service.PreviewAsync(id, file.FileName, content, cancellationToken);
    }

    [EndpointSummary("Apply uploaded XLSX (phase 2)")]
    public static async Task<ImportResultDto> ApplyImport(IGradebookImportService service,
        Guid id,
        Guid uploadId,
        ApplyImportRequest request,
        CancellationToken cancellationToken)
    {
        return await service.ApplyAsync(id, uploadId, request, cancellationToken);
    }

    [EndpointSummary("Uploaded XLSX files of the lesson")]
    public static async Task<IReadOnlyList<ImportFileDto>> ListImports(IGradebookImportService service, Guid id,
        CancellationToken cancellationToken)
    {
        return await service.ListImportsAsync(id, cancellationToken);
    }

    [EndpointSummary("Download an uploaded XLSX file")]
    public static async Task<FileContentHttpResult> DownloadImport(IGradebookImportService service,
        Guid id,
        Guid uploadId,
        CancellationToken cancellationToken)
    {
        var file = await service.DownloadImportAsync(id, uploadId, cancellationToken);
        return TypedResults.File(file.Content, file.ContentType, file.FileName);
    }
}
