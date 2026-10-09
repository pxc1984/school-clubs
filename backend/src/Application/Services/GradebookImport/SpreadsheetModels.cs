using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Application.Services.GradebookImport;

/// <summary>Что положить в трафарет. <see cref="LessonId"/> пуст у чистого трафарета группы.</summary>
public record GradebookTemplate(
    Guid GroupId,
    string GroupTitle,
    Guid? LessonId,
    DateOnly? Date,
    string? Topic,
    IReadOnlyList<TemplateTask> Tasks,
    IReadOnlyList<TemplateStudent> Students,
    IReadOnlyList<TemplateEntry> Entries,
    DateTimeOffset GeneratedAt);

/// <param name="Id"><c>null</c> в чистом трафарете группы: задачи ещё не созданы.</param>
public record TemplateTask(Guid? Id, string Number, string? Title);

public record TemplateStudent(Guid Id, string Name);

public record TemplateEntry(Guid StudentId, Guid TaskId, GradebookEntryTaskStatus Status);

/// <summary>Содержимое листа «Журнал» как оно есть, без сопоставления с данными системы.</summary>
/// <param name="Meta"><c>null</c> — «чужой файл»: листа <c>_meta</c> нет.</param>
public record ParsedGradebook(ParsedMeta? Meta, IReadOnlyList<ParsedColumn> Columns, IReadOnlyList<ParsedRow> Rows);

public record ParsedMeta(int FormatVersion, Guid? LessonId, Guid? GroupId, DateTimeOffset? GeneratedAt);

/// <param name="Number">Номер из заголовка «Задача N», если он есть.</param>
public record ParsedColumn(string Letter, Guid? TaskId, string? Number, string Header);

/// <param name="Row">Номер строки на листе (с 1).</param>
public record ParsedRow(int Row, Guid? StudentId, string Name, IReadOnlyList<ParsedCell> Cells);

/// <param name="Text">Непустой текст ячейки как его видит человек.</param>
public record ParsedCell(string ColumnLetter, string Address, string Text);

/// <summary>Файл не удаётся прочитать как трафарет: не xlsx, нет листа, превышены лимиты.</summary>
public class InvalidSpreadsheetException(string message) : ArgumentException(message);

/// <summary>Генерация и разбор XLSX-трафарета. Реализация на ClosedXML — в Infrastructure.</summary>
public interface IGradebookSpreadsheet
{
    public const int FormatVersion = 1;
    public const int MaxRows = 500;
    public const int MaxTasks = 50;

    byte[] Generate(GradebookTemplate template);

    /// <exception cref="InvalidSpreadsheetException"/>
    ParsedGradebook Parse(Stream content);
}
