using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Common.Exceptions;

namespace SchoolClubs.Domain.Entities.Users;

public class Parent : User
{
    public Parent(string? firstName = null,
        string? middleName = null,
        string? lastName = null,
        string email = "invalid@school218.ru",
        string? phone = null,
        IList<Student>? children = null) : base(firstName, middleName, lastName, email, phone)
    {
        UniqueListElementsException.ThrowIfHasDuplicates(children);
        Children = children ?? [];
    }

    public IList<Student> Children;
}
