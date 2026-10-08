using JetBrains.Annotations;

namespace SchoolClubs.Domain.Entities.Gradebook;

/// <summary>Задача занятия. Создаётся через <see cref="Lesson.AddTask"/>.</summary>
public class Task
{
    private readonly List<GradebookEntry> _entries = [];

    internal Task(Lesson lesson, long order)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order);

        Order = order;
        LessonId = lesson.Id;
        Lesson = lesson;
    }

    [UsedImplicitly]
    private Task()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Порядковый номер задачи в занятии, начиная с 1.</summary>
    public long Order { get; internal set; }

    public IReadOnlyCollection<GradebookEntry> Entries
    {
        get { return _entries; }
    }

    public Guid LessonId { get; private set; }
    public Lesson Lesson { get; private set; } = null!;

    internal void AttachEntry(GradebookEntry entry)
    {
        _entries.Add(entry);
    }

    internal void DetachEntry(GradebookEntry entry)
    {
        _entries.Remove(entry);
    }
}
