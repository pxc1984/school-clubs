using System.Globalization;
using FluentValidation.Results;
using SchoolClubs.Application.Common;
using SchoolClubs.Application.Common.Exceptions;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Data;
using SchoolClubs.Domain.Entities.Gradebook;
using Task = System.Threading.Tasks.Task;

namespace SchoolClubs.Application.Services.GradebookImport;

public class GradebookImportService(
    TeacherAccess access,
    ILessonRepository lessons,
    ILessonGradebookRepository gradebooks,
    IDataEntryRepository dataEntries,
    IFileStorage storage,
    IGradebookSpreadsheet spreadsheet,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IGradebookImportService
{
    public const long MaxFileSize = 2 * 1024 * 1024;
    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    public async Task<FileDto> GetLessonTemplateAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        var gradebook = await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken);

        var template = new GradebookTemplate(group.Id, group.Title, lesson.Id, lesson.Date, lesson.Topic,
            lesson.Tasks.OrderBy(t => t.Order).Select(t => new TemplateTask(t.Id, t.Number, t.Title)).ToList(),
            Students(group),
            gradebook?.GradebookEntries
                .Where(e => group.HasStudent(e.StudentId))
                .Select(e => new TemplateEntry(e.StudentId, e.TaskId, e.Status))
                .ToList() ?? [],
            timeProvider.GetUtcNow());
        return new FileDto($"{SafeFileName(group.Title)}_{lesson.Date:yyyy-MM-dd}.xlsx", XlsxContentType,
            spreadsheet.Generate(template));
    }

    public async Task<FileDto> GetGroupTemplateAsync(Guid groupId, int taskCount,
        CancellationToken cancellationToken = default)
    {
        if (taskCount is < 1 or > IGradebookSpreadsheet.MaxTasks)
            throw new ArgumentException($"tasks should be between 1 and {IGradebookSpreadsheet.MaxTasks}",
                nameof(taskCount));

        var group = await access.GetGroupAsync(groupId, cancellationToken);
        var template = new GradebookTemplate(group.Id, group.Title, null, null, null,
            Enumerable.Range(1, taskCount)
                .Select(n => new TemplateTask(null, n.ToString(CultureInfo.InvariantCulture), null))
                .ToList(),
            Students(group), [], timeProvider.GetUtcNow());
        return new FileDto($"{SafeFileName(group.Title)}_трафарет.xlsx", XlsxContentType,
            spreadsheet.Generate(template));
    }

    public async Task<ImportPreviewDto> PreviewAsync(Guid lessonId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        var bytes = await ReadValidatedAsync(fileName, content, cancellationToken);
        var parsed = spreadsheet.Parse(new MemoryStream(bytes));

        // Файл хранится бессрочно, даже если импорт так и не применят.
        var uploadId = Guid.NewGuid();
        var location = storage.LocationOf(KeyOf(lessonId, uploadId));
        await storage.PutAsync(location, new MemoryStream(bytes), XlsxContentType, cancellationToken);
        await dataEntries.AddAsync(new DataEntry(location, Path.GetFileName(fileName), XlsxContentType, bytes.Length),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var gradebook = await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken);
        var plan = GradebookImportPlanner.Plan(parsed, lesson, group, gradebook);
        return new ImportPreviewDto(uploadId, Path.GetFileName(fileName), plan.ForeignFile,
            plan.LessonMismatch ? await DescribeMismatchAsync(plan.OtherLessonId) : null,
            plan.Changes, plan.UnrecognizedRows, plan.MissingStudents, plan.MissingTaskNumbers, plan.CellErrors,
            plan.Warnings, plan.Errors);
    }

    public async Task<ImportResultDto> ApplyAsync(Guid lessonId,
        Guid uploadId,
        ApplyImportRequest request,
        CancellationToken cancellationToken = default)
    {
        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        var entry = await FindUploadAsync(lessonId, uploadId, cancellationToken);
        var parsed = spreadsheet.Parse(new MemoryStream(await storage.ReadAsync(entry.Location, cancellationToken)));

        var mappings = request.RowMappings?
            .GroupBy(m => m.Row)
            .ToDictionary(g => g.Key, g => g.Last().StudentId);
        var gradebook = await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken);
        var plan = GradebookImportPlanner.Plan(parsed, lesson, group, gradebook, mappings);

        if (plan.Errors.Count > 0)
            throw new ValidationException(plan.Errors.Select(e => new ValidationFailure("File", e)));
        if (plan.LessonMismatch && !request.ConfirmLessonMismatch)
            throw new ConflictException("the file belongs to another lesson; confirm to apply it here");

        if (request.CreateMissingTasks && plan.MissingTaskNumbers.Count > 0)
        {
            if (lesson.Tasks.Count + plan.MissingTaskNumbers.Count > IGradebookSpreadsheet.MaxTasks)
                throw new ArgumentException($"lesson can't have more than {IGradebookSpreadsheet.MaxTasks} tasks");
            foreach (var number in plan.MissingTaskNumbers)
                lesson.AddTask(number: number);
            plan = GradebookImportPlanner.Plan(parsed, lesson, group, gradebook, mappings);
        }

        if (gradebook is null)
        {
            gradebook = new LessonGradebook(lesson);
            await gradebooks.AddAsync(gradebook, cancellationToken);
        }

        var created = 0;
        var updated = 0;
        foreach (var change in plan.Changes.Where(c => c.TaskId is not null && c.Kind != ImportChangeKind.Unchanged))
        {
            var task = lesson.Tasks.First(t => t.Id == change.TaskId);
            var student = group.Students.First(s => s.Id == change.StudentId);
            // Замечание, поставленное в сетке, импорт не стирает.
            var comment = gradebook.FindEntry(student.Id, task.Id)?.Comment;
            gradebook.SetEntry(student, task, change.Status, comment);
            if (change.Kind == ImportChangeKind.New) created++;
            else updated++;
        }

        gradebook.AttachFile(entry);
        if (request.Publish)
            gradebook.Publish(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportResultDto(created, updated, plan.Changes.Count(c => c.Kind == ImportChangeKind.Unchanged),
            gradebook.IsPublished);
    }

    public async Task<IReadOnlyList<ImportFileDto>> ListImportsAsync(Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        await access.GetLessonAsync(lessonId, cancellationToken);
        var gradebook = await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken);
        var location = storage.LocationOf(KeyOf(lessonId, Guid.Empty));
        var prefix = location.Key[..(location.Key.LastIndexOf('/') + 1)];

        var entries = await dataEntries.ListByKeyPrefixAsync(location.Bucket, prefix, cancellationToken);
        return entries
            .Select(e => new ImportFileDto(UploadIdOf(e.Location), e.OriginalFileName, e.SizeBytes, e.UploadedAt,
                gradebook?.DataEntryId == e.Id))
            .OrderByDescending(f => f.UploadedAt)
            .ToList();
    }

    public async Task<FileDto> DownloadImportAsync(Guid lessonId, Guid uploadId,
        CancellationToken cancellationToken = default)
    {
        await access.GetLessonAsync(lessonId, cancellationToken);
        var entry = await FindUploadAsync(lessonId, uploadId, cancellationToken);
        return new FileDto(entry.OriginalFileName, entry.ContentType,
            await storage.ReadAsync(entry.Location, cancellationToken));
    }

    private async Task<DataEntry> FindUploadAsync(Guid lessonId, Guid uploadId, CancellationToken cancellationToken)
    {
        var entry = await dataEntries.FindByLocationAsync(storage.LocationOf(KeyOf(lessonId, uploadId)),
            cancellationToken);
        Guard.Against.NotFound(uploadId, entry);
        return entry;
    }

    private async Task<string> DescribeMismatchAsync(Guid? otherLessonId)
    {
        if (otherLessonId is { } id && await lessons.FindByIdAsync(id) is { } other)
            return $"Файл от занятия {other.Date:dd.MM.yyyy}";
        return "Файл от другого занятия или группы";
    }

    /// <summary>Лимиты из дизайна: 2 МБ, только .xlsx с zip-сигнатурой.</summary>
    private static async Task<byte[]> ReadValidatedAsync(string fileName, Stream content,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetExtension(fileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidSpreadsheetException("Нужен файл .xlsx.");

        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > MaxFileSize)
                throw new InvalidSpreadsheetException("Файл больше 2 МБ.");
        }

        var bytes = buffer.ToArray();
        if (bytes.Length < ZipSignature.Length || !bytes.AsSpan(0, ZipSignature.Length).SequenceEqual(ZipSignature))
            throw new InvalidSpreadsheetException("Файл не похож на .xlsx.");
        return bytes;
    }

    private static IReadOnlyList<TemplateStudent> Students(Group group)
    {
        return group.Students
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => new TemplateStudent(s.Id, $"{s.LastName} {s.FirstName}"))
            .ToList();
    }

    private static string KeyOf(Guid lessonId, Guid uploadId)
    {
        return $"gradebooks/{lessonId}/{uploadId}.xlsx";
    }

    private static Guid UploadIdOf(S3Location location)
    {
        return Guid.TryParse(Path.GetFileNameWithoutExtension(location.Key), out var id) ? id : Guid.Empty;
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Trim().Select(c => invalid.Contains(c) || char.IsWhiteSpace(c) ? '_' : c));
    }
}
