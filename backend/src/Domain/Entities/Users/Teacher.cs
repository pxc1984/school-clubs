using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Common.Exceptions;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Domain.Entities.Users;

public class Teacher : User
{
    public Teacher(string? firstName = null,
        string? middleName = null,
        string? lastName = null,
        string email = "invalid@school218.ru",
        string? phone = null,
        IList<Group>? linkedGroups = null) : base(firstName, middleName, lastName, email, phone)
    {
        UniqueListElementsException.ThrowIfHasDuplicates(linkedGroups);

        LinkedGroups = linkedGroups ?? [];
    }

    private IList<Group> LinkedGroups;
}
