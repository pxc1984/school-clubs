using JetBrains.Annotations;
using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Common.Exceptions;

namespace SchoolClubs.Domain.Entities.Users;

public abstract class Profile : IAuditableEntity
{
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
    public string FirstName { get; private set; } = "";
    public string MiddleName { get; private set; } = "";
    public string LastName { get; private set; } = "";

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }

    public void Rename(string? firstName, string? middleName = null, string? lastName = null)
    {
        NamePartsException.ThrowIfHasParts(firstName);
        NamePartsException.ThrowIfHasParts(middleName);
        NamePartsException.ThrowIfHasParts(lastName);

        FirstName = firstName ?? "";
        MiddleName = middleName ?? "";
        LastName = lastName ?? "";
    }
}
