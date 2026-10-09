using System.Text.Json.Serialization;
using JetBrains.Annotations;
using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Entities.Gradebook;

/// <remarks>В JSON — строкой (<c>"Absent"</c>).</remarks>
[JsonConverter(typeof(JsonStringEnumConverter<AttendanceStatus>))]
public enum AttendanceStatus
{
    /// <summary>Не был на занятии — отметок по задачам не ждём.</summary>
    Absent,

    /// <summary>Опоздал — часть задач может остаться без отметок.</summary>
    Late,
}

/// <summary>
/// Отметка посещаемости ученика на занятии. Создаётся через <see cref="LessonGradebook.SetAttendance"/>.
/// Отсутствие записи — обычное присутствие.
/// </summary>
public class LessonAttendance : IAuditableEntity
{
    internal LessonAttendance(Guid lessonId, Student student, AttendanceStatus status)
    {
        LessonId = lessonId;
        StudentId = student.Id;
        Student = student;
        ChangeStatus(status);
    }

    [UsedImplicitly]
    private LessonAttendance()
    {
    }

    public Guid LessonId { get; private set; }

    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = null!;

    public AttendanceStatus Status { get; private set; }

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }

    internal void ChangeStatus(AttendanceStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status), status, "unknown attendance status");

        Status = status;
    }
}
