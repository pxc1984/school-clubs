using SchoolClubs.Application.Services.Lessons;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Application.Services.Gradebook;

/// <param name="InGroup"><c>false</c> — выбывший ученик, у которого остались отметки на этом занятии.</param>
public record GradebookStudentDto(
    Guid Id,
    string LastName,
    string FirstName,
    string MiddleName,
    string? Grade,
    bool InGroup);

public record GradebookCellDto(Guid StudentId, Guid TaskId, GradebookEntryTaskStatus Status, string? Comment);

/// <summary>Сетка «ученики × задачи» занятия. Журнала может ещё не быть — тогда ячеек нет.</summary>
public record GradebookDto(
    Guid LessonId,
    DateOnly Date,
    string? Topic,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<TaskDto> Tasks,
    IReadOnlyList<GradebookStudentDto> Students,
    IReadOnlyList<GradebookCellDto> Entries);

/// <param name="Status"><c>null</c> — очистить ячейку.</param>
/// <param name="Comment">Замечание; ячейка задаётся целиком, <c>null</c> убирает замечание.</param>
public record GradebookCellChange(Guid StudentId, Guid TaskId, GradebookEntryTaskStatus? Status, string? Comment);

/// <summary>Только изменённые ячейки: при работе двух учителей выигрывает последняя запись в ячейку.</summary>
public record SaveGradebookRequest(IReadOnlyList<GradebookCellChange> Changes);
