using JetBrains.Annotations;
using SchoolClubs.Domain.Common;

namespace SchoolClubs.Domain.Entities.Gradebook;

public class Lesson : IAuditableEntity
{
    private static readonly TimeOnly EarliestLessonTime = new(7, 0);
    private static readonly TimeOnly LatestLessonTime = new(22, 0);

    private readonly List<Task> _tasks = [];

    public Lesson(Group group, DateOnly date, TimeOnly? time)
    {
        ArgumentNullException.ThrowIfNull(group);

        Reschedule(date, time);

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

    public Guid ParentGroupId { get; private set; }
    public Group ParentGroup { get; private set; } = null!;

    /// <summary>Задачи занятия; порядок задаёт <see cref="Task.Order"/>.</summary>
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

    /// <summary>Добавляет задачу в конец списка задач занятия.</summary>
    public Task AddTask()
    {
        var order = _tasks.Count == 0 ? 1 : _tasks.Max(t => t.Order) + 1;
        var task = new Task(this, order);
        _tasks.Add(task);
        return task;
    }

    /// <summary>Удаляет задачу и сдвигает порядковые номера следующих за ней.</summary>
    /// <returns><c>false</c>, если задача не относится к занятию.</returns>
    public bool RemoveTask(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
        if (existing == null)
            return false;

        _tasks.Remove(existing);
        foreach (var next in _tasks.Where(t => t.Order > existing.Order))
            next.Order--;
        return true;
    }
}
