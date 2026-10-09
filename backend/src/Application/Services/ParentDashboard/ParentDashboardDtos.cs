using SchoolClubs.Application.Services.Common;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Application.Services.ParentDashboard;

/// <summary>Карточка ребёнка на главной родителя.</summary>
public record ChildDto(
    Guid Id,
    string LastName,
    string FirstName,
    string MiddleName,
    string? Grade,
    IReadOnlyList<ChildGroupDto> Groups);

/// <summary>Группа ребёнка с краткой сводкой за всё время.</summary>
public record ChildGroupDto(
    Guid Id,
    string Title,
    bool IsArchived,
    int LessonsAttended,
    int LessonsTotal,
    LastLessonDto? LastLesson,
    GroupPosition? Position);

/// <summary>«Решил 4 из 5, в среднем по группе 3,2»; <c>Attended = false</c> — «не был на занятии».</summary>
/// <param name="Attendance">Отметка учителя «не был» / «опоздал»; <c>null</c> — её нет.</param>
public record LastLessonDto(
    Guid LessonId,
    DateOnly Date,
    string? Topic,
    bool Attended,
    AttendanceStatus? Attendance,
    int Solved,
    int TaskCount,
    double? GroupAverageSolved);

/// <summary>Сводка ребёнка в группе за период.</summary>
/// <param name="Score">Взвешенный балл за период 0..1; <c>null</c>, если не был ни на одном занятии.</param>
/// <param name="PerfectStreak">«3 занятия подряд решил всё»; <c>null</c>, если серия короче 2.</param>
public record ChildSummaryDto(
    StudentDto Student,
    GroupRefDto Group,
    DateOnly? From,
    DateOnly? To,
    int LessonsAttended,
    int LessonsTotal,
    int TasksSolved,
    int TasksTotal,
    double? Score,
    GroupPosition? Position,
    int? PerfectStreak,
    IReadOnlyList<ChartPointDto> Chart);

/// <summary>Точка графика по занятию: балл ребёнка (null — не был) против медианы и полосы 25–75% группы.</summary>
public record ChartPointDto(
    Guid LessonId,
    DateOnly Date,
    double? Score,
    double? GroupMedian,
    double? GroupP25,
    double? GroupP75);

/// <summary>Занятие в ленте, новые сверху.</summary>
/// <param name="Attendance">Отметка учителя «не был» / «опоздал»; <c>null</c> — её нет.</param>
public record ChildLessonDto(
    Guid LessonId,
    DateOnly Date,
    TimeOnly? Time,
    string? Topic,
    bool Attended,
    AttendanceStatus? Attendance,
    int Solved,
    int TaskCount,
    double? Score,
    double? GroupAverageSolved,
    IReadOnlyList<ChildTaskDto> Tasks);

/// <summary>Задача занятия: отметка и замечание ребёнку, «решили 12 из 15» по группе (null — мало посетивших).</summary>
public record ChildTaskDto(
    Guid TaskId,
    string Number,
    string? Title,
    GradebookEntryTaskStatus? Status,
    string? Comment,
    int? GroupSolved,
    int? GroupAttended);
