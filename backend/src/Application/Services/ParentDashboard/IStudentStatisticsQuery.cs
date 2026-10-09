using SchoolClubs.Application.Services.Common;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Application.Services.ParentDashboard;

/// <summary>Опубликованное занятие группы с хотя бы одной задачей.</summary>
public record PublishedLessonRow(
    Guid Id,
    DateOnly Date,
    TimeOnly? Time,
    string? Topic,
    IReadOnlyList<LessonTaskRow> Tasks);

public record LessonTaskRow(Guid Id, long Order, string? Title);

/// <summary>Отметка без замечания: для агрегатов по группе комментарии не нужны.</summary>
public record MarkRow(Guid StudentId, Guid LessonId, Guid TaskId, GradebookEntryTaskStatus Status);

public record GroupPeriodData(IReadOnlyList<PublishedLessonRow> Lessons, IReadOnlyList<MarkRow> Marks);

/// <summary>
/// Сырые данные для статистики родителя. Учитываются только опубликованные журналы;
/// дальше всё считает <see cref="GroupStatistics"/> в памяти.
/// </summary>
public interface IStudentStatisticsQuery
{
    /// <summary>Группы ученика: где он сейчас состоит или имеет отметки в опубликованных журналах.</summary>
    Task<IReadOnlyList<GroupRefDto>> ListGroupsAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>Опубликованные занятия группы за период (границы включительно) и все отметки на них.</summary>
    Task<GroupPeriodData> LoadAsync(Guid groupId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    /// <summary>Замечания учителя ученику на занятиях: id задачи → текст.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetCommentsAsync(Guid studentId,
        IReadOnlyCollection<Guid> lessonIds,
        CancellationToken cancellationToken = default);
}
