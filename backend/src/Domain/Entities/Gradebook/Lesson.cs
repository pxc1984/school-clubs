using JetBrains.Annotations;
using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Common.Exceptions;

namespace SchoolClubs.Domain.Entities.Gradebook;

public class Lesson : IAuditableEntity
{
    private static readonly TimeOnly EarliestLessonTime = new(7, 0);
    private static readonly TimeOnly LatestLessonTime = new(22, 0);

    public const int TopicMaxLength = 200;

    private readonly List<Task> _tasks = [];

    public Lesson(Group group, DateOnly date, TimeOnly? time, string? topic = null)
    {
        ArgumentNullException.ThrowIfNull(group);

        Reschedule(date, time);
        ChangeTopic(topic);

        ParentGroupId = group.Id;
        ParentGroup = group;
    }

    [UsedImplicitly]
    private Lesson()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public DateOnly Date { get; private set; }
    public TimeOnly? Time { get; private set; }

    /// <summary>Тема занятия — её видит родитель в ленте занятий.</summary>
    public string? Topic { get; private set; }

    public Guid ParentGroupId { get; private set; }
    public Group ParentGroup { get; private set; } = null!;

    /// <summary>Задачи занятия; порядок задаёт <see cref="Task.Order"/> по номерам.</summary>
    public IReadOnlyCollection<Task> Tasks
    {
        get { return _tasks; }
    }

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }

    public void Reschedule(DateOnly date, TimeOnly? time)
    {
        if (time < EarliestLessonTime || time > LatestLessonTime)
            throw new ArgumentException($"Time not in range {EarliestLessonTime}-{LatestLessonTime}", nameof(time));

        Date = date;
        Time = time;
    }

    public void ChangeTopic(string? topic)
    {
        var trimmed = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim();
        if (trimmed?.Length > TopicMaxLength)
            throw new ArgumentException($"topic shouldn't be longer than {TopicMaxLength}", nameof(topic));

        Topic = trimmed;
    }

    /// <summary>Добавляет задачу; без номера — следующий целый после имеющихся («1.1», «1.2» → «2»).</summary>
    /// <exception cref="ArgumentException">Номер некорректен или уже занят.</exception>
    public Task AddTask(string? title = null, string? number = null)
    {
        var task = new Task(this, number ?? TaskNumber.Next(_tasks.Select(t => t.Number)), title);
        EnsureNumberIsFree(task.Number, null);
        _tasks.Add(task);
        Reorder();
        return task;
    }

    /// <summary>Меняет номер задачи; порядок задач следует за номерами.</summary>
    /// <exception cref="ArgumentException">Номер некорректен или занят другой задачей.</exception>
    public void RenumberTask(Task task, string number)
    {
        ArgumentNullException.ThrowIfNull(task);
        var existing = _tasks.FirstOrDefault(t => t.Id == task.Id)
                       ?? throw new ArgumentException("task doesn't belong to the lesson", nameof(task));

        var normalized = TaskNumber.Normalize(number);
        EnsureNumberIsFree(normalized, existing);
        existing.Number = normalized;
        Reorder();
    }

    public Task? FindTaskByNumber(string number)
    {
        return _tasks.FirstOrDefault(t => TaskNumber.AreSame(t.Number, number.Trim()));
    }

    /// <summary>Удаляет задачу; номера остальных не меняются, позиции сдвигаются.</summary>
    /// <returns><c>false</c>, если задача не относится к занятию.</returns>
    public bool RemoveTask(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
        if (existing == null)
            return false;

        _tasks.Remove(existing);
        Reorder();
        return true;
    }

    private void EnsureNumberIsFree(string number, Task? except)
    {
        if (_tasks.Any(t => t != except && TaskNumber.AreSame(t.Number, number)))
            throw new DuplicateTaskNumberException(number);
    }

    /// <summary>При равных номерах (старые данные) сохраняется прежний порядок.</summary>
    private void Reorder()
    {
        var ordered = _tasks
            .OrderBy(t => t.Number, TaskNumber.Comparer)
            .ThenBy(t => t.Order == 0 ? long.MaxValue : t.Order)
            .ToList();
        for (var i = 0; i < ordered.Count; i++)
            if (ordered[i].Order != i + 1)
                ordered[i].Order = i + 1;
    }
}
