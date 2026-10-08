using System.Text.Json.Serialization;
using JetBrains.Annotations;
using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Entities.Gradebook;

/// <remarks>В JSON — строкой (<c>"SolvedWithRemarks"</c>).</remarks>
[JsonConverter(typeof(JsonStringEnumConverter<GradebookEntryTaskStatus>))]
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
    public const int CommentMaxLength = 500;

    internal GradebookEntry(Lesson lesson,
        Student student,
        Task task,
        GradebookEntryTaskStatus status,
        string? comment)
    {
        LessonId = lesson.Id;
        Lesson = lesson;
        StudentId = student.Id;
        Student = student;
        TaskId = task.Id;
        Task = task;
        ChangeStatus(status);
        ChangeComment(comment);
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

    /// <summary>Замечание учителя к решению — сама «обратная связь» родителю.</summary>
    public string? Comment { get; private set; }

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

    internal void ChangeComment(string? comment)
    {
        var trimmed = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (trimmed?.Length > CommentMaxLength)
            throw new ArgumentException($"comment shouldn't be longer than {CommentMaxLength}", nameof(comment));

        Comment = trimmed;
    }
}
