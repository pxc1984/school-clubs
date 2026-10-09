using System.Text.Json.Serialization;
using SchoolClubs.Application.Common.Options;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Application.Services.ParentDashboard;

/// <summary>Положение в группе за период; ниже половины явно не пишем.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<GroupPosition>))]
public enum GroupPosition
{
    Top10,
    Top25,
    AboveHalf,
}

/// <summary>Результат посетившего занятие ученика.</summary>
/// <param name="Solved">Решённые задачи (<see cref="GradebookEntry.IsSolved"/>) — для «решил 4 из 5».</param>
/// <param name="Weight">Сумма весов отметок — для балла.</param>
public record StudentLessonResult(int Solved, double Weight, int TaskCount)
{
    /// <summary>Балл на занятии от 0 до 1.</summary>
    public double Score
    {
        get { return Weight / TaskCount; }
    }

    public bool SolvedAll
    {
        get { return Solved == TaskCount; }
    }
}

public record StudentPeriodResult(int LessonsAttended, int Solved, double Weight, int TaskCount)
{
    /// <summary>Балл за период: сумма весов / сумма задач на посещённых занятиях; <c>null</c>, если не был ни разу.</summary>
    public double? Score
    {
        get { return TaskCount == 0 ? null : Weight / TaskCount; }
    }
}

/// <summary>Агрегаты одного занятия. Пороги приватности применяются здесь, а не в API.</summary>
public class LessonStatistics
{
    private readonly Dictionary<Guid, int> _solvedByTask;
    private readonly Dictionary<Guid, AttendanceStatus> _attendance;

    internal LessonStatistics(PublishedLessonRow lesson,
        Dictionary<Guid, StudentLessonResult> results,
        Dictionary<Guid, int> solvedByTask,
        Dictionary<Guid, AttendanceStatus> attendance)
    {
        Lesson = lesson;
        Results = results;
        _solvedByTask = solvedByTask;
        _attendance = attendance;
    }

    public PublishedLessonRow Lesson { get; }

    /// <summary>
    /// Только посетившие: у ученика есть хотя бы одна отметка или он отмечен «опоздал».
    /// Отметка «не был» исключает ученика, даже если отметки по задачам остались.
    /// </summary>
    public IReadOnlyDictionary<Guid, StudentLessonResult> Results { get; }

    public int TaskCount
    {
        get { return Lesson.Tasks.Count; }
    }

    public StudentLessonResult? ResultOf(Guid studentId)
    {
        return Results.GetValueOrDefault(studentId);
    }

    /// <summary><c>null</c> — особых отметок нет (был вовремя или не отмечен).</summary>
    public AttendanceStatus? AttendanceOf(Guid studentId)
    {
        return _attendance.TryGetValue(studentId, out var status) ? status : null;
    }

    /// <summary>Среднее число решённых задач; только при ≥ 3 посетивших кроме самого ученика.</summary>
    public double? AverageSolved(Guid studentId)
    {
        if (Peers(studentId) < GroupStatistics.MinPeersForAverage)
            return null;

        return Results.Values.Average(r => r.Solved);
    }

    /// <summary>Медиана балла группы; порог тот же, что у среднего.</summary>
    public double? MedianScore(Guid studentId)
    {
        if (Peers(studentId) < GroupStatistics.MinPeersForAverage)
            return null;

        return GroupStatistics.Quantile(Scores(), 0.5);
    }

    /// <summary>Полоса 25–75 перцентиль балла; только при ≥ 5 посетивших.</summary>
    public (double P25, double P75)? ScoreBand()
    {
        if (Results.Count < GroupStatistics.MinAttendeesForBand)
            return null;

        var scores = Scores();
        return (GroupStatistics.Quantile(scores, 0.25), GroupStatistics.Quantile(scores, 0.75));
    }

    /// <summary>«Задачу решили 5 из 14»; только при ≥ 3 посетивших.</summary>
    public (int Solved, int Attended)? TaskSolvedBy(Guid taskId)
    {
        if (Results.Count < GroupStatistics.MinAttendeesForTaskStats)
            return null;

        return (_solvedByTask.GetValueOrDefault(taskId), Results.Count);
    }

    private int Peers(Guid studentId)
    {
        return Results.Count - (Results.ContainsKey(studentId) ? 1 : 0);
    }

    private List<double> Scores()
    {
        return Results.Values.Select(r => r.Score).Order().ToList();
    }
}

/// <summary>
/// Статистика группы за период — чистая функция от опубликованных занятий и отметок.
/// Пропуск занятия не считается нулём: в балл идут только посещённые занятия.
/// Опоздавший посетил занятие; его неотмеченные задачи считаются нерешёнными.
/// </summary>
public class GroupStatistics
{
    public const int MinPeersForAverage = 3;
    public const int MinAttendeesForTaskStats = 3;
    public const int MinAttendeesForBand = 5;
    public const int MinLessonsForRanking = 3;
    public const int MinRankedForPosition = 5;
    public const int MinStreak = 2;

    private const double Epsilon = 1e-9;

    private GroupStatistics(IReadOnlyList<LessonStatistics> lessons)
    {
        Lessons = lessons;
    }

    /// <summary>Занятия по возрастанию даты и времени.</summary>
    public IReadOnlyList<LessonStatistics> Lessons { get; }

    public static GroupStatistics Compute(GroupPeriodData data, StatisticsWeights weights)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(weights);

        var marksByLesson = data.Marks.ToLookup(m => m.LessonId);
        var attendanceByLesson = data.Attendance.ToLookup(a => a.LessonId);
        var lessons = new List<LessonStatistics>();
        foreach (var lesson in data.Lessons
                     .Where(l => l.Tasks.Count > 0)
                     .OrderBy(l => l.Date)
                     .ThenBy(l => l.Time))
        {
            var taskIds = lesson.Tasks.Select(t => t.Id).ToHashSet();
            var attendance = attendanceByLesson[lesson.Id].ToDictionary(a => a.StudentId, a => a.Status);
            var marks = marksByLesson[lesson.Id]
                .Where(m => taskIds.Contains(m.TaskId))
                .Where(m => attendance.GetValueOrDefault(m.StudentId, AttendanceStatus.Late) != AttendanceStatus.Absent)
                .ToList();
            var results = marks
                .GroupBy(m => m.StudentId)
                .ToDictionary(g => g.Key, g => new StudentLessonResult(
                    g.Count(m => IsSolved(m.Status)),
                    g.Sum(m => weights.Of(m.Status)),
                    lesson.Tasks.Count));
            foreach (var (studentId, status) in attendance)
            {
                if (status == AttendanceStatus.Late)
                    results.TryAdd(studentId, new StudentLessonResult(0, 0, lesson.Tasks.Count));
            }

            var solvedByTask = marks
                .Where(m => IsSolved(m.Status))
                .GroupBy(m => m.TaskId)
                .ToDictionary(g => g.Key, g => g.Count());
            lessons.Add(new LessonStatistics(lesson, results, solvedByTask, attendance));
        }

        return new GroupStatistics(lessons);
    }

    public StudentPeriodResult PeriodOf(Guid studentId)
    {
        var attended = Lessons.Select(l => l.ResultOf(studentId)).OfType<StudentLessonResult>().ToList();
        return new StudentPeriodResult(attended.Count, attended.Sum(r => r.Solved), attended.Sum(r => r.Weight),
            attended.Sum(r => r.TaskCount));
    }

    /// <summary>
    /// Перцентиль — доля учеников с баллом за период ниже или равным (mid-rank для ничьих)
    /// среди посетивших ≥ 3 занятий. <c>null</c>, если ранжировать некого (меньше 5) или ученик не в ранжировании.
    /// </summary>
    public double? PercentileOf(Guid studentId)
    {
        var ranked = Lessons
            .SelectMany(l => l.Results)
            .GroupBy(r => r.Key)
            .Where(g => g.Count() >= MinLessonsForRanking)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Value.Weight) / g.Sum(r => r.Value.TaskCount));
        if (ranked.Count < MinRankedForPosition || !ranked.TryGetValue(studentId, out var score))
            return null;

        var below = ranked.Values.Count(s => s < score - Epsilon);
        var equal = ranked.Values.Count(s => Math.Abs(s - score) <= Epsilon);
        return (below + 0.5 * equal) / ranked.Count;
    }

    public GroupPosition? PositionOf(Guid studentId)
    {
        return PercentileOf(studentId) switch
        {
            >= 0.9 - Epsilon => GroupPosition.Top10,
            >= 0.75 - Epsilon => GroupPosition.Top25,
            > 0.5 + Epsilon => GroupPosition.AboveHalf,
            _ => null,
        };
    }

    /// <summary>
    /// Сколько последних посещённых занятий подряд ученик решил всё; пропуски серию не рвут.
    /// <c>null</c>, если серия короче 2.
    /// </summary>
    public int? PerfectStreakOf(Guid studentId)
    {
        var streak = 0;
        for (var i = Lessons.Count - 1; i >= 0; i--)
        {
            var result = Lessons[i].ResultOf(studentId);
            if (result is null)
                continue;
            if (!result.SolvedAll)
                break;

            streak++;
        }

        return streak >= MinStreak ? streak : null;
    }

    /// <summary>Квантиль отсортированной выборки с линейной интерполяцией (как в Excel PERCENTILE.INC).</summary>
    public static double Quantile(IReadOnlyList<double> sorted, double p)
    {
        if (sorted.Count == 0)
            throw new ArgumentException("empty sample", nameof(sorted));

        var position = (sorted.Count - 1) * p;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
    }

    private static bool IsSolved(GradebookEntryTaskStatus status)
    {
        return status is GradebookEntryTaskStatus.Solved or GradebookEntryTaskStatus.SolvedWithRemarks;
    }
}
