using Microsoft.Extensions.Options;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Application.Common.Options;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Services.ParentDashboard;

public class ParentDashboardService(
    IUserRepository users,
    IStudentStatisticsQuery statistics,
    ICurrentUser currentUser,
    IOptions<StatisticsOptions> options) : IParentDashboardService
{
    public async Task<IReadOnlyList<ChildDto>> ListChildrenAsync(CancellationToken cancellationToken = default)
    {
        var parent = await LoadParentAsync(cancellationToken);
        // Братья и сёстры часто ходят в одну группу — считаем её один раз.
        var computed = new Dictionary<Guid, GroupStatistics>();
        var children = new List<ChildDto>();
        foreach (var child in parent.Children.OrderBy(c => c.LastName).ThenBy(c => c.FirstName))
        {
            var groups = new List<ChildGroupDto>();
            foreach (var group in await statistics.ListGroupsAsync(child.Id, cancellationToken))
            {
                if (!computed.TryGetValue(group.Id, out var stats))
                {
                    stats = await ComputeAsync(group.Id, null, null, cancellationToken);
                    computed[group.Id] = stats;
                }

                groups.Add(ToGroupCard(group, child.Id, stats));
            }

            children.Add(new ChildDto(child.Id, child.LastName, child.FirstName, child.MiddleName, child.Grade,
                groups));
        }

        return children;
    }

    public async Task<ChildSummaryDto> GetSummaryAsync(Guid studentId,
        Guid groupId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var (child, group) = await AuthorizeAsync(studentId, groupId, from, to, cancellationToken);
        var stats = await ComputeAsync(groupId, from, to, cancellationToken);
        var period = stats.PeriodOf(studentId);
        var chart = stats.Lessons
            .Select(l =>
            {
                var band = l.ScoreBand();
                return new ChartPointDto(l.Lesson.Id, l.Lesson.Date, l.ResultOf(studentId)?.Score,
                    l.MedianScore(studentId), band?.P25, band?.P75);
            })
            .ToList();

        return new ChildSummaryDto(child.ToDto(), group, from, to, period.LessonsAttended, stats.Lessons.Count,
            period.Solved, period.TaskCount, period.Score, stats.PositionOf(studentId),
            stats.PerfectStreakOf(studentId), chart);
    }

    public async Task<IReadOnlyList<ChildLessonDto>> ListLessonsAsync(Guid studentId,
        Guid groupId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync(studentId, groupId, from, to, cancellationToken);
        var data = await statistics.LoadAsync(groupId, from, to, cancellationToken);
        var stats = GroupStatistics.Compute(data, options.Value.Weights);
        var attendedIds = stats.Lessons
            .Where(l => l.ResultOf(studentId) is not null)
            .Select(l => l.Lesson.Id)
            .ToList();
        var comments = await statistics.GetCommentsAsync(studentId, attendedIds, cancellationToken);
        var statuses = data.Marks
            .Where(m => m.StudentId == studentId)
            .ToDictionary(m => m.TaskId, m => m.Status);

        return stats.Lessons
            .Reverse()
            .Select(l =>
            {
                var result = l.ResultOf(studentId);
                var tasks = l.Lesson.Tasks
                    .Select(t =>
                    {
                        var solvedBy = l.TaskSolvedBy(t.Id);
                        return new ChildTaskDto(t.Id, t.Number, t.Title,
                            statuses.TryGetValue(t.Id, out var status) ? status : null,
                            comments.GetValueOrDefault(t.Id), solvedBy?.Solved, solvedBy?.Attended);
                    })
                    .ToList();
                return new ChildLessonDto(l.Lesson.Id, l.Lesson.Date, l.Lesson.Time, l.Lesson.Topic,
                    result is not null, l.AttendanceOf(studentId), result?.Solved ?? 0, l.TaskCount, result?.Score, l.AverageSolved(studentId),
                    tasks);
            })
            .ToList();
    }

    private static ChildGroupDto ToGroupCard(GroupRefDto group, Guid studentId, GroupStatistics stats)
    {
        var period = stats.PeriodOf(studentId);
        LastLessonDto? lastLesson = null;
        if (stats.Lessons.Count > 0)
        {
            var last = stats.Lessons[^1];
            var result = last.ResultOf(studentId);
            lastLesson = new LastLessonDto(last.Lesson.Id, last.Lesson.Date, last.Lesson.Topic, result is not null,
                last.AttendanceOf(studentId), result?.Solved ?? 0, last.TaskCount, last.AverageSolved(studentId));
        }

        return new ChildGroupDto(group.Id, group.Title, group.IsArchived, period.LessonsAttended,
            stats.Lessons.Count, lastLesson, stats.PositionOf(studentId));
    }

    private async Task<GroupStatistics> ComputeAsync(Guid groupId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var data = await statistics.LoadAsync(groupId, from, to, cancellationToken);
        return GroupStatistics.Compute(data, options.Value.Weights);
    }

    /// <summary>Чужой ребёнок и группа, где ребёнок не учился, — как несуществующие (404).</summary>
    private async Task<(Student Child, GroupRefDto Group)> AuthorizeAsync(Guid studentId,
        Guid groupId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (from > to)
            throw new ArgumentException("'from' must not be later than 'to'", nameof(from));

        var parent = await LoadParentAsync(cancellationToken);
        var child = parent.Children.FirstOrDefault(c => c.Id == studentId)
                    ?? throw new NotFoundException(studentId.ToString(), nameof(Student));
        var group = (await statistics.ListGroupsAsync(studentId, cancellationToken)).FirstOrDefault(g => g.Id == groupId)
                    ?? throw new NotFoundException(groupId.ToString(), "Group");
        return (child, group);
    }

    private async Task<User> LoadParentAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        return await users.GetWithChildrenAsync(userId, cancellationToken) ?? throw new UnauthorizedAccessException();
    }
}
