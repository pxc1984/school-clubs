using Microsoft.EntityFrameworkCore;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Application.Services.ParentDashboard;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Infrastructure.Data.Queries;

/// <summary>Чтение для статистики родителя: только проекции, без трекинга агрегатов.</summary>
public class StudentStatisticsQuery(DbContext dbContext) : IStudentStatisticsQuery
{
    public async Task<IReadOnlyList<GroupRefDto>> ListGroupsAsync(Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var publishedLessonIds = dbContext.Set<LessonGradebook>()
            .Where(g => g.PublishedAt != null)
            .Select(g => g.LessonId);
        var groupsWithMarks = dbContext.Set<GradebookEntry>()
            .Where(e => e.StudentId == studentId && publishedLessonIds.Contains(e.LessonId))
            .Select(e => e.Lesson.ParentGroupId);
        var lessonsWithAttendance = dbContext.Set<LessonAttendance>()
            .Where(a => a.StudentId == studentId && publishedLessonIds.Contains(a.LessonId))
            .Select(a => a.LessonId);
        var groupsWithAttendance = dbContext.Set<Lesson>()
            .Where(l => lessonsWithAttendance.Contains(l.Id))
            .Select(l => l.ParentGroupId);

        return await dbContext.Set<Group>()
            .Where(g => g.Students.Any(s => s.Id == studentId)
                        || groupsWithMarks.Contains(g.Id)
                        || groupsWithAttendance.Contains(g.Id))
            .OrderBy(g => g.IsArchived)
            .ThenBy(g => g.Title)
            .Select(g => new GroupRefDto(g.Id, g.Title, g.IsArchived))
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupPeriodData> LoadAsync(Guid groupId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var lessons = dbContext.Set<Lesson>()
            .Where(l => l.ParentGroupId == groupId)
            .Where(l => dbContext.Set<LessonGradebook>().Any(g => g.LessonId == l.Id && g.PublishedAt != null));
        if (from is { } start)
            lessons = lessons.Where(l => l.Date >= start);
        if (to is { } end)
            lessons = lessons.Where(l => l.Date <= end);

        var lessonRows = await lessons
            .Where(l => l.Tasks.Any())
            .OrderBy(l => l.Date)
            .ThenBy(l => l.Time)
            .Select(l => new PublishedLessonRow(l.Id, l.Date, l.Time, l.Topic,
                l.Tasks
                    .OrderBy(t => t.Order)
                    .Select(t => new LessonTaskRow(t.Id, t.Number, t.Title))
                    .ToList()))
            .ToListAsync(cancellationToken);

        var lessonIds = lessonRows.Select(l => l.Id).ToList();
        var marks = await dbContext.Set<GradebookEntry>()
            .Where(e => lessonIds.Contains(e.LessonId))
            .Select(e => new MarkRow(e.StudentId, e.LessonId, e.TaskId, e.Status))
            .ToListAsync(cancellationToken);
        var attendance = await dbContext.Set<LessonAttendance>()
            .Where(a => lessonIds.Contains(a.LessonId))
            .Select(a => new AttendanceRow(a.StudentId, a.LessonId, a.Status))
            .ToListAsync(cancellationToken);

        return new GroupPeriodData(lessonRows, marks, attendance);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetCommentsAsync(Guid studentId,
        IReadOnlyCollection<Guid> lessonIds,
        CancellationToken cancellationToken = default)
    {
        if (lessonIds.Count == 0)
            return new Dictionary<Guid, string>();

        return await dbContext.Set<GradebookEntry>()
            .Where(e => e.StudentId == studentId && lessonIds.Contains(e.LessonId) && e.Comment != null)
            .ToDictionaryAsync(e => e.TaskId, e => e.Comment!, cancellationToken);
    }
}
