using JetBrains.Annotations;

namespace SchoolClubs.Domain.Entities.Gradebook;

/// <summary>Задача занятия. Создаётся через <see cref="Lesson.AddTask"/>.</summary>
public class Task
{
    public const int TitleMaxLength = 200;

    private readonly List<GradebookEntry> _entries = [];

    internal Task(Lesson lesson, long order, string? title = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order);

        Order = order;
        LessonId = lesson.Id;
        Lesson = lesson;
        Rename(title);
    }

    [UsedImplicitly]
    private Task()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Порядковый номер задачи в занятии, начиная с 1.</summary>
    public long Order { get; internal set; }

    /// <summary>Необязательное название: «Задача 2: Рекурсия» в сетке и трафарете.</summary>
    public string? Title { get; private set; }

    public IReadOnlyCollection<GradebookEntry> Entries
    {
        get { return _entries; }
    }

    public Guid LessonId { get; private set; }
    public Lesson Lesson { get; private set; } = null!;

    public void Rename(string? title)
    {
        var trimmed = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        if (trimmed?.Length > TitleMaxLength)
            throw new ArgumentException($"title shouldn't be longer than {TitleMaxLength}", nameof(title));

        Title = trimmed;
    }

    internal void AttachEntry(GradebookEntry entry)
    {
        _entries.Add(entry);
    }

    internal void DetachEntry(GradebookEntry entry)
    {
        _entries.Remove(entry);
    }
}
