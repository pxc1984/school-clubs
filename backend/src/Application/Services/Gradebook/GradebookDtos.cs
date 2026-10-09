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

/// <summary>Отметка посещаемости; ученики без неё были на занятии вовремя.</summary>
public record GradebookAttendanceDto(Guid StudentId, AttendanceStatus Status);

/// <summary>Сетка «ученики × задачи» занятия. Журнала может ещё не быть — тогда ячеек нет.</summary>
public record GradebookDto(
    Guid LessonId,
    DateOnly Date,
    string? Topic,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<TaskDto> Tasks,
    IReadOnlyList<GradebookStudentDto> Students,
    IReadOnlyList<GradebookCellDto> Entries,
    IReadOnlyList<GradebookAttendanceDto> Attendance);

/// <param name="Status"><c>null</c> — очистить ячейку.</param>
/// <param name="Comment">Замечание; ячейка задаётся целиком, <c>null</c> убирает замечание.</param>
public record GradebookCellChange(Guid StudentId, Guid TaskId, GradebookEntryTaskStatus? Status, string? Comment);

/// <param name="Status"><c>null</c> — был вовремя (снять отметку).</param>
public record GradebookAttendanceChange(Guid StudentId, AttendanceStatus? Status);

/// <summary>Только изменённые ячейки: при работе двух учителей выигрывает последняя запись в ячейку.</summary>
/// <param name="Attendance">Изменения посещаемости; <c>null</c> — без изменений.</param>
public record SaveGradebookRequest(
    IReadOnlyList<GradebookCellChange> Changes,
    IReadOnlyList<GradebookAttendanceChange>? Attendance = null);
