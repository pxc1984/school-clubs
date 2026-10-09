using JetBrains.Annotations;
using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Entities.Data;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Entities.Gradebook;

/// <summary>
/// Журнал занятия — агрегат над записями <see cref="GradebookEntry"/>.
/// Гарантирует не более одной записи на пару (ученик, задача) и что задачи относятся к этому занятию.
/// Хранит и посещаемость: «не был» / «опоздал» — не более одной отметки на ученика.
/// </summary>
public class LessonGradebook : IAuditableEntity
{
    private readonly List<GradebookEntry> _gradebookEntries = [];
    private readonly List<LessonAttendance> _attendance = [];

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

    /// <summary>Пока <c>null</c>, журнал — черновик и родители его не видят.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    public bool IsPublished
    {
        get { return PublishedAt is not null; }
    }

    public IReadOnlyCollection<GradebookEntry> GradebookEntries
    {
        get { return _gradebookEntries; }
    }

    /// <summary>Только отклонения от обычного присутствия.</summary>
    public IReadOnlyCollection<LessonAttendance> Attendance
    {
        get { return _attendance; }
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

    /// <summary>Открывает журнал родителям. Повторная публикация сохраняет исходную дату.</summary>
    public void Publish(DateTimeOffset publishedAt)
    {
        PublishedAt ??= publishedAt;
    }

    /// <summary>Снимает с публикации — для крупной переделки журнала.</summary>
    public void Unpublish()
    {
        PublishedAt = null;
    }

    /// <summary>Создаёт запись для пары (ученик, задача) или обновляет статус и замечание существующей.</summary>
    public GradebookEntry SetEntry(Student student,
        Task task,
        GradebookEntryTaskStatus status,
        string? comment = null)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(task);
        if (task.LessonId != LessonId)
            throw new ArgumentException("task belongs to another lesson", nameof(task));

        var entry = FindEntry(student.Id, task.Id);
        if (entry != null)
        {
            entry.ChangeStatus(status);
            entry.ChangeComment(comment);
            return entry;
        }

        entry = new GradebookEntry(Lesson, student, task, status, comment);
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

    /// <summary>Ставит или меняет отметку посещаемости ученика.</summary>
    /// <remarks>Отметки по задачам не трогает: опоздавший мог что-то решить.</remarks>
    public LessonAttendance SetAttendance(Student student, AttendanceStatus status)
    {
        ArgumentNullException.ThrowIfNull(student);

        var attendance = FindAttendance(student.Id);
        if (attendance != null)
        {
            attendance.ChangeStatus(status);
            return attendance;
        }

        attendance = new LessonAttendance(LessonId, student, status);
        _attendance.Add(attendance);
        return attendance;
    }

    /// <summary>Снимает отметку посещаемости — ученик был вовремя.</summary>
    /// <returns><c>false</c>, если отметки не было.</returns>
    public bool RemoveAttendance(Guid studentId)
    {
        var attendance = FindAttendance(studentId);
        return attendance != null && _attendance.Remove(attendance);
    }

    public LessonAttendance? FindAttendance(Guid studentId)
    {
        return _attendance.FirstOrDefault(a => a.StudentId == studentId);
    }

    /// <summary>
    /// Переносит отметки дубля <paramref name="from"/> на <paramref name="to"/> (слияние учеников).
    /// Если у <paramref name="to"/> уже есть отметка по той же задаче, остаётся она.
    /// Посещаемость переносится так же: приоритет у отметки <paramref name="to"/>.
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

            SetEntry(to, entry.Task, entry.Status, entry.Comment);
            moved++;
        }

        if (FindAttendance(from.Id) is { } attendance)
        {
            RemoveAttendance(from.Id);
            if (FindAttendance(to.Id) == null)
                SetAttendance(to, attendance.Status);
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
