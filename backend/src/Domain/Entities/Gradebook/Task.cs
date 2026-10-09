using JetBrains.Annotations;

namespace SchoolClubs.Domain.Entities.Gradebook;

/// <summary>Задача занятия. Создаётся через <see cref="Lesson.AddTask"/>.</summary>
public class Task
{
    public const int TitleMaxLength = 200;

    private readonly List<GradebookEntry> _entries = [];

    internal Task(Lesson lesson, string number, string? title = null)
    {
        Number = TaskNumber.Normalize(number);
        LessonId = lesson.Id;
        Lesson = lesson;
        Rename(title);
    }

    [UsedImplicitly]
    private Task()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Номер в свободной форме, уникален в занятии: «1», «1.1», «2а». См. <see cref="TaskNumber"/>.</summary>
    public string Number { get; internal set; } = null!;

    /// <summary>
    /// Позиция в занятии (с 1) по естественному порядку номеров; пересчитывает <see cref="Lesson"/>,
    /// чтобы сортировать в запросах без разбора номеров.
    /// </summary>
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
