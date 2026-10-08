using JetBrains.Annotations;
using SchoolClubs.Domain.Common.Exceptions;

namespace SchoolClubs.Domain.Entities.Users;

public class Parent : User
{
    private readonly List<Student> _children = [];

    public Parent(string? firstName = null,
        string? middleName = null,
        string? lastName = null,
        string email = "invalid@school218.ru",
        string? phone = null,
        IList<Student>? children = null) : base(firstName, middleName, lastName, email, phone)
    {
        UniqueListElementsException.ThrowIfHasDuplicates(children);

        foreach (var child in children ?? [])
            AddChild(child);
    }

    [UsedImplicitly]
    private Parent()
    {
    }

    public IReadOnlyCollection<Student> Children
    {
        get { return _children; }
    }

    /// <returns><c>false</c>, если ребёнок уже привязан.</returns>
    public bool AddChild(Student child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (_children.Any(c => c.Id == child.Id))
            return false;

        _children.Add(child);
        return true;
    }

    /// <returns><c>false</c>, если ребёнок не был привязан.</returns>
    public bool RemoveChild(Student child)
    {
        return _children.RemoveAll(c => c.Id == child.Id) > 0;
    }
}
