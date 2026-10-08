using JetBrains.Annotations;
using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Entities.Data;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Entities.Gradebook;

/// <summary>
/// Журнал занятия — агрегат над записями <see cref="GradebookEntry"/>.
/// Гарантирует не более одной записи на пару (ученик, задача) и что задачи относятся к этому занятию.
/// </summary>
public class LessonGradebook : IAuditableEntity
{
    private readonly List<GradebookEntry> _gradebookEntries = [];

    public LessonGradebook(Lesson lesson, DataEntry? dataEntry = null)
    {
        ArgumentNullException.ThrowIfNull(lesson);

        LessonId = lesson.Id;
        Lesson = lesson;
        AttachFile(dataEntry);
    }

    [UsedImplicitly]
    private LessonGradebook()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid LessonId { get; private set; }
    public Lesson Lesson { get; private set; } = null!;

    public Guid? DataEntryId { get; private set; }
    public DataEntry? StoredFile { get; private set; }

    public IReadOnlyCollection<GradebookEntry> GradebookEntries
    {
        get { return _gradebookEntries; }
    }

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }

    /// <summary>Привязывает (или отвязывает при <c>null</c>) файл журнала.</summary>
    public void AttachFile(DataEntry? dataEntry)
    {
        DataEntryId = dataEntry?.Id;
        StoredFile = dataEntry;
    }

    /// <summary>Создаёт запись для пары (ученик, задача) или обновляет статус существующей.</summary>
    public GradebookEntry SetEntry(Student student, Task task, GradebookEntryTaskStatus status)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(task);
        if (task.LessonId != LessonId)
            throw new ArgumentException("task belongs to another lesson", nameof(task));

        var entry = FindEntry(student.Id, task.Id);
        if (entry != null)
        {
            entry.ChangeStatus(status);
            return entry;
        }

        entry = new GradebookEntry(Lesson, student, task, status);
        _gradebookEntries.Add(entry);
        task.AttachEntry(entry);
        return entry;
    }

    /// <returns><c>false</c>, если записи не было.</returns>
    public bool RemoveEntry(Guid studentId, Guid taskId)
    {
        var entry = FindEntry(studentId, taskId);
        if (entry == null)
            return false;

        _gradebookEntries.Remove(entry);
        entry.Task?.DetachEntry(entry);
        return true;
    }

    /// <summary>
    /// Переносит отметки дубля <paramref name="from"/> на <paramref name="to"/> (слияние учеников).
    /// Если у <paramref name="to"/> уже есть отметка по той же задаче, остаётся она.
    /// </summary>
    /// <returns>Сколько отметок перенесено.</returns>
    public int MoveEntries(Student from, Student to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        if (from.Id == to.Id)
            throw new ArgumentException("can't move entries to the same student", nameof(to));

        var moved = 0;
        foreach (var entry in EntriesOf(from.Id))
        {
            RemoveEntry(from.Id, entry.TaskId);
            if (FindEntry(to.Id, entry.TaskId) != null)
                continue;

            SetEntry(to, entry.Task, entry.Status);
            moved++;
        }

        return moved;
    }

    public GradebookEntry? FindEntry(Guid studentId, Guid taskId)
    {
        return _gradebookEntries.FirstOrDefault(e => e.StudentId == studentId && e.TaskId == taskId);
    }

    public IReadOnlyList<GradebookEntry> EntriesOf(Guid studentId)
    {
        return _gradebookEntries.Where(e => e.StudentId == studentId).ToList();
    }
}
