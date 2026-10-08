using SchoolClubs.Application.Services.Common;

namespace SchoolClubs.Application.Services.TeacherGroups;

/// <param name="UnfilledLessonCount">Прошедшие занятия (включая сегодня) с неполностью заполненным журналом.</param>
public record TeacherGroupCardDto(
    Guid Id,
    string Title,
    string? Description,
    int StudentCount,
    DateOnly? LastLessonDate,
    DateOnly? NextLessonDate,
    int UnfilledLessonCount);

/// <param name="FilledPercent">Доля заполненных ячеек «ученик × задача» для текущего состава, 0–100.</param>
public record LessonSummaryDto(
    Guid Id,
    DateOnly Date,
    TimeOnly? Time,
    string? Topic,
    int TaskCount,
    int FilledPercent,
    bool IsPublished);

public record TeacherGroupDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsArchived,
    IReadOnlyList<StudentDto> Students,
    IReadOnlyList<LessonSummaryDto> Lessons);

public record SummaryLessonDto(Guid Id, DateOnly Date, string? Topic, int TaskCount, bool IsPublished);

/// <param name="InGroup"><c>false</c> — выбывший ученик, у которого остались отметки.</param>
public record SummaryStudentDto(
    Guid Id,
    string LastName,
    string FirstName,
    string MiddleName,
    string? Grade,
    bool InGroup);

/// <summary>Ячейка матрицы есть, только если у ученика есть отметки на занятии.</summary>
/// <param name="Marked">Сколько задач отмечено.</param>
/// <param name="Solved">Сколько из них решено (в том числе с замечаниями).</param>
public record SummaryCellDto(Guid StudentId, Guid LessonId, int Marked, int Solved);

/// <summary>Матрица «ученики × занятия» группы.</summary>
public record GroupMatrixDto(
    IReadOnlyList<SummaryLessonDto> Lessons,
    IReadOnlyList<SummaryStudentDto> Students,
    IReadOnlyList<SummaryCellDto> Cells);
