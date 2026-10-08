using JetBrains.Annotations;
using SchoolClubs.Domain.Common.Exceptions;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Domain.Entities.Users;

public class Teacher : User
{
    private readonly List<Group> _linkedGroups = [];

    public Teacher(string? firstName = null,
        string? middleName = null,
        string? lastName = null,
        string email = "invalid@school218.ru",
        string? phone = null,
        IList<Group>? linkedGroups = null) : base(firstName, middleName, lastName, email, phone)
    {
        UniqueListElementsException.ThrowIfHasDuplicates(linkedGroups);

        foreach (var group in linkedGroups ?? [])
            group.AddTeacher(this);
    }

    [UsedImplicitly]
    private Teacher()
    {
    }

    /// <summary>Группы, которые ведёт учитель. Изменяется через <see cref="Group.AddTeacher"/>.</summary>
    public IReadOnlyCollection<Group> LinkedGroups
    {
        get { return _linkedGroups; }
    }

    internal void LinkGroup(Group group)
    {
        if (_linkedGroups.All(g => g.Id != group.Id))
            _linkedGroups.Add(group);
    }

    internal void UnlinkGroup(Group group)
    {
        _linkedGroups.RemoveAll(g => g.Id == group.Id);
    }
}
