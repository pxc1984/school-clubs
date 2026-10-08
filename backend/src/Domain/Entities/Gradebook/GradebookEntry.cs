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

public class GradebookEntry(Lesson lesson, Student student, Task task, GradebookEntryTaskStatus status) : IAuditableEntity
{
    public Guid LessonId = lesson.Id;
    public Lesson Lesson = lesson;

    public Guid StudentId = student.Id;
    public Student Student = student;

    public Guid TaskId = task.Id;
    public Task Task = task;

    public GradebookEntryTaskStatus Status = status;

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }
}
