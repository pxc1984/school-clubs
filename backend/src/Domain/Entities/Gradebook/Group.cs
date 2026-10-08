using JetBrains.Annotations;
using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Entities.Gradebook;

public class Group : IAuditableEntity
{
    private readonly List<Teacher> _teachers = [];
    private readonly List<Student> _students = [];

    public Group(string title, string? description = null)
    {
        Rename(title, description);
    }

    [UsedImplicitly]
    private Group()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public IReadOnlyCollection<Teacher> Teachers
    {
        get { return _teachers; }
    }

    public IReadOnlyCollection<Student> Students
    {
        get { return _students; }
    }

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }

    public void Rename(string title, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>Привязывает учителя к группе (обе стороны связи).</summary>
    /// <returns><c>false</c>, если учитель уже привязан.</returns>
    public bool AddTeacher(Teacher teacher)
    {
        ArgumentNullException.ThrowIfNull(teacher);
        if (_teachers.Any(t => t.Id == teacher.Id))
            return false;

        _teachers.Add(teacher);
        teacher.LinkGroup(this);
        return true;
    }

    /// <returns><c>false</c>, если учитель не был привязан.</returns>
    public bool RemoveTeacher(Teacher teacher)
    {
        ArgumentNullException.ThrowIfNull(teacher);
        if (_teachers.RemoveAll(t => t.Id == teacher.Id) == 0)
            return false;

        teacher.UnlinkGroup(this);
        return true;
    }

    /// <returns><c>false</c>, если ученик уже в группе.</returns>
    public bool AddStudent(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        if (HasStudent(student.Id))
            return false;

        _students.Add(student);
        return true;
    }

    /// <returns><c>false</c>, если ученика не было в группе.</returns>
    public bool RemoveStudent(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        return _students.RemoveAll(s => s.Id == student.Id) > 0;
    }

    public bool HasStudent(Guid studentId)
    {
        return _students.Any(s => s.Id == studentId);
    }
}
