namespace SchoolClubs.Application.Services.GradebookImport;

public interface IGradebookImportService
{
    /// <summary>Трафарет занятия с уже проставленными отметками: <c>&lt;Группа&gt;_&lt;yyyy-MM-dd&gt;.xlsx</c>.</summary>
    Task<FileDto> GetLessonTemplateAsync(Guid lessonId, CancellationToken cancellationToken = default);

    /// <summary>Чистый трафарет группы с N задачами — его можно загрузить в любое занятие группы.</summary>
    Task<FileDto> GetGroupTemplateAsync(Guid groupId, int taskCount, CancellationToken cancellationToken = default);

    /// <summary>Фаза 1: сохраняет файл в хранилище и показывает, что изменит импорт.</summary>
    Task<ImportPreviewDto> PreviewAsync(Guid lessonId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>Фаза 2: перечитывает сохранённый файл и применяет его с ручными сопоставлениями.</summary>
    Task<ImportResultDto> ApplyAsync(Guid lessonId,
        Guid uploadId,
        ApplyImportRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImportFileDto>> ListImportsAsync(Guid lessonId, CancellationToken cancellationToken = default);

    /// <summary>Исходник любой загрузки, в том числе неприменённой.</summary>
    Task<FileDto> DownloadImportAsync(Guid lessonId, Guid uploadId, CancellationToken cancellationToken = default);
}
