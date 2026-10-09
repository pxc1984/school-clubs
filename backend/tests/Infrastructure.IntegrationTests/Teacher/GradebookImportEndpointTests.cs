using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Services.Gradebook;
using SchoolClubs.Application.Services.GradebookImport;
using SchoolClubs.Application.Services.Lessons;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;
using Task = System.Threading.Tasks.Task;

namespace SchoolClubs.Infrastructure.IntegrationTests.Teacher;

/// <summary>Двухфазный импорт XLSX через HTTP, с файлами в настоящем S3 (RustFS).</summary>
public class GradebookImportEndpointTests : TestBase
{
    private const string Teacher = "/api/v1/teacher";

    private HttpClient _teacher = null!;
    private Group _group = null!;
    private Student _alice = null!;
    private Student _bob = null!;
    private LessonDto _lesson = null!;

    [SetUp]
    public async Task Seed()
    {
        User teacherUser;
        (_teacher, teacherUser) = await LoginAsAsync(UserRole.Teacher);
        _group = new Group("Робототехника 5");
        _alice = new Student("Алиса", lastName: "Андреева");
        _bob = new Student("Борис", lastName: "Борисов");
        _group.AddStudent(_alice);
        _group.AddStudent(_bob);
        await InScope(async sp =>
        {
            var teacher = await sp.GetRequiredService<IUserRepository>().GetWithGroupsAsync(teacherUser.Id);
            _group.AddTeacher(teacher!);
            await sp.GetRequiredService<IGroupRepository>().AddAsync(_group);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });

        _lesson = (await (await _teacher.PostAsJsonAsync($"{Teacher}/groups/{_group.Id}/lessons",
                new CreateLessonRequest(new DateOnly(2026, 10, 1), null, "Датчики", 2)))
            .Content.ReadFromJsonAsync<LessonDto>())!;
    }

    [TearDown]
    public void DisposeTeacher()
    {
        _teacher.Dispose();
    }

    private async Task<byte[]> DownloadTemplateAsync()
    {
        var response = await _teacher.GetAsync($"{Teacher}/lessons/{_lesson.Id}/gradebook/template");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe(GradebookImportService.XlsxContentType);
        response.Content.Headers.ContentDisposition!.FileNameStar.ShouldBe("Робототехника_5_2026-10-01.xlsx");
        return await response.Content.ReadAsByteArrayAsync();
    }

    private static byte[] Fill(byte[] template, Action<IXLWorksheet> fill)
    {
        using var workbook = new XLWorkbook(new MemoryStream(template));
        var sheet = workbook.Worksheet("Журнал");
        fill(sheet);
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private async Task<HttpResponseMessage> UploadAsync(byte[] file, string name = "journal.xlsx", Guid? lessonId = null)
    {
        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue(GradebookImportService.XlsxContentType);
        form.Add(content, "file", name);
        return await _teacher.PostAsync($"{Teacher}/lessons/{lessonId ?? _lesson.Id}/gradebook/import", form);
    }

    private async Task<ImportPreviewDto> PreviewAsync(byte[] file)
    {
        var response = await UploadAsync(file);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ImportPreviewDto>())!;
    }

    private async Task<HttpResponseMessage> ApplyAsync(Guid uploadId, ApplyImportRequest request, Guid? lessonId = null)
    {
        return await _teacher.PostAsJsonAsync(
            $"{Teacher}/lessons/{lessonId ?? _lesson.Id}/gradebook/import/{uploadId}/apply", request);
    }

    private async Task<GradebookDto> GradebookAsync()
    {
        return (await _teacher.GetFromJsonAsync<GradebookDto>($"{Teacher}/lessons/{_lesson.Id}/gradebook"))!;
    }

    [Test]
    public async Task Template_PreviewApply_KeepsCommentsAndStoresFile()
    {
        await _teacher.PutAsJsonAsync($"{Teacher}/lessons/{_lesson.Id}/gradebook", new SaveGradebookRequest(
            [new GradebookCellChange(_alice.Id, _lesson.Tasks[0].Id, GradebookEntryTaskStatus.Unsolved, "подумать")]));

        var file = Fill(await DownloadTemplateAsync(), sheet =>
        {
            sheet.Cell("C3").Value = "+";   // Алиса, задача 1: было «-»
            sheet.Cell("D3").Value = "да";  // Алиса, задача 2: новая
            sheet.Cell("C4").Value = "x";   // Борис: ошибка ячейки
        });

        var preview = await PreviewAsync(file);
        preview.ForeignFile.ShouldBeFalse();
        preview.LessonMismatch.ShouldBeNull();
        preview.Changes.Select(c => (c.StudentId, c.TaskNumber, c.Kind)).ShouldBe(
            [(_alice.Id, "1", ImportChangeKind.Changed), (_alice.Id, "2", ImportChangeKind.New)]);
        preview.CellErrors.ShouldBe(["C4: «x»"]);
        preview.MissingStudents.ShouldBeEmpty();

        // До применения журнал не меняется.
        (await GradebookAsync()).Entries.ShouldHaveSingleItem().Status.ShouldBe(GradebookEntryTaskStatus.Unsolved);

        var result = await (await ApplyAsync(preview.UploadId, new ApplyImportRequest(false, false, true, null)))
            .Content.ReadFromJsonAsync<ImportResultDto>();
        result.ShouldBe(new ImportResultDto(1, 1, 0, true));

        var gradebook = await GradebookAsync();
        gradebook.PublishedAt.ShouldNotBeNull();
        var first = gradebook.Entries.Single(e => e.TaskId == _lesson.Tasks[0].Id);
        first.Status.ShouldBe(GradebookEntryTaskStatus.Solved);
        first.Comment.ShouldBe("подумать");

        var imports = await _teacher.GetFromJsonAsync<List<ImportFileDto>>($"{Teacher}/lessons/{_lesson.Id}/gradebook/imports");
        var stored = imports.ShouldHaveSingleItem();
        stored.UploadId.ShouldBe(preview.UploadId);
        stored.Attached.ShouldBeTrue();
        var original = await _teacher.GetByteArrayAsync(
            $"{Teacher}/lessons/{_lesson.Id}/gradebook/imports/{preview.UploadId}/file");
        original.ShouldBe(file);
    }

    [Test]
    public async Task Upload_RejectsNonXlsx()
    {
        (await UploadAsync("hello"u8.ToArray(), "notes.txt")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await UploadAsync("hello"u8.ToArray(), "fake.xlsx")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await UploadAsync(new byte[GradebookImportService.MaxFileSize + 1], "big.xlsx"))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task FileOfAnotherLesson_NeedsConfirmation()
    {
        var file = Fill(await DownloadTemplateAsync(), sheet => sheet.Cell("C3").Value = "+");
        var other = (await (await _teacher.PostAsJsonAsync($"{Teacher}/groups/{_group.Id}/lessons",
                new CreateLessonRequest(new DateOnly(2026, 10, 8), null, null, 2)))
            .Content.ReadFromJsonAsync<LessonDto>())!;

        var response = await UploadAsync(file, lessonId: other.Id);
        var preview = (await response.Content.ReadFromJsonAsync<ImportPreviewDto>())!;
        preview.LessonMismatch.ShouldBe("Файл от занятия 01.10.2026");

        (await ApplyAsync(preview.UploadId, new ApplyImportRequest(false, false, false, null), other.Id))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ApplyAsync(preview.UploadId, new ApplyImportRequest(true, false, false, null), other.Id))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GroupTemplate_WithMoreTasks_CreatesThemOnApply()
    {
        var response = await _teacher.GetAsync($"{Teacher}/groups/{_group.Id}/gradebook-template?tasks=4");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var file = Fill(await response.Content.ReadAsByteArrayAsync(), sheet =>
        {
            sheet.Cell("F3").Value = "+"; // Алиса, задача 4
            sheet.Cell("C4").Value = "-"; // Борис, задача 1
        });

        var preview = await PreviewAsync(file);
        preview.LessonMismatch.ShouldBeNull();
        preview.MissingTaskNumbers.ShouldBe(["3", "4"]);

        await ApplyAsync(preview.UploadId, new ApplyImportRequest(false, true, false, null));

        var gradebook = await GradebookAsync();
        gradebook.Tasks.Count.ShouldBe(4);
        gradebook.Entries.Count.ShouldBe(2);
        gradebook.Entries.ShouldContain(e => e.StudentId == _alice.Id && e.TaskId == gradebook.Tasks[3].Id);
    }

    [Test]
    public async Task UnrecognizedRow_ManualMapping_DuplicateBlocks()
    {
        var file = Fill(await DownloadTemplateAsync(), sheet =>
        {
            sheet.Cell("A5").Value = "";
            sheet.Cell("B5").Value = "Борька";
            sheet.Cell("C5").Value = "+";
        });

        var preview = await PreviewAsync(file);
        var row = preview.UnrecognizedRows.ShouldHaveSingleItem();
        row.Row.ShouldBe(5);

        // Ручное сопоставление на ученика, который уже есть в файле, — дубль.
        (await ApplyAsync(preview.UploadId,
                new ApplyImportRequest(false, false, false, [new RowMappingRequest(5, _bob.Id)])))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Пропуск строки — можно.
        var result = await (await ApplyAsync(preview.UploadId,
                new ApplyImportRequest(false, false, false, [new RowMappingRequest(5, null)])))
            .Content.ReadFromJsonAsync<ImportResultDto>();
        result!.Created.ShouldBe(0);
    }

    [Test]
    public async Task ForeignTeacher_CannotDownloadOrImport()
    {
        var (other, _) = await LoginAsAsync(UserRole.Teacher);
        using var _ = other;

        (await other.GetAsync($"{Teacher}/lessons/{_lesson.Id}/gradebook/template"))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await other.GetAsync($"{Teacher}/lessons/{_lesson.Id}/gradebook/imports"))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
