using JetBrains.Annotations;
using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Entities.Gradebook;

public enum GradebookEntryTaskStatus
{
    UnsolvedWithRemarks,
    Unsolved,
    SolvedWithRemarks,
    Solved,
}

/// <summary>Отметка о решении задачи учеником. Создаётся через <see cref="LessonGradebook.SetEntry"/>.</summary>
public class GradebookEntry : IAuditableEntity
{
    internal GradebookEntry(Lesson lesson, Student student, Task task, GradebookEntryTaskStatus status)
    {
        LessonId = lesson.Id;
        Lesson = lesson;
        StudentId = student.Id;
        Student = student;
        TaskId = task.Id;
        Task = task;
        ChangeStatus(status);
    }

    [UsedImplicitly]
    private GradebookEntry()
    {
    }

    public Guid LessonId { get; private set; }
    public Lesson Lesson { get; private set; } = null!;

    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = null!;

    public Guid TaskId { get; private set; }
    public Task Task { get; private set; } = null!;

    public GradebookEntryTaskStatus Status { get; private set; }

    public bool IsSolved
    {
        get { return Status is GradebookEntryTaskStatus.Solved or GradebookEntryTaskStatus.SolvedWithRemarks; }
    }

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }

    internal void ChangeStatus(GradebookEntryTaskStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status), status, "unknown task status");

        Status = status;
    }
}
