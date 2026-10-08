using JetBrains.Annotations;
using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Common.Exceptions;

namespace SchoolClubs.Domain.Entities.Users;

public abstract class Profile : IAuditableEntity
{
    private const string DefaultFirstName = "Неизвестно";
    private const string DefaultMiddleName = "Неизвестнов";
    private const string DefaultLastName = "Неизвестнович";

    protected Profile(string? firstName,
        string? middleName = null,
        string? lastName = null)
    {
        Rename(firstName, middleName, lastName);
    }

    [UsedImplicitly]
    protected Profile()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string FirstName { get; private set; } = DefaultFirstName;
    public string MiddleName { get; private set; } = DefaultMiddleName;
    public string LastName { get; private set; } = DefaultLastName;

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }

    public void Rename(string? firstName, string? middleName = null, string? lastName = null)
    {
        NamePartsException.ThrowIfHasParts(firstName);
        NamePartsException.ThrowIfHasParts(middleName);
        NamePartsException.ThrowIfHasParts(lastName);

        FirstName = firstName ?? DefaultFirstName;
        MiddleName = middleName ?? DefaultMiddleName;
        LastName = lastName ?? DefaultLastName;
    }
}
