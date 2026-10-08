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
        NamePartsException.ThrowIfHasParts(firstName);
        NamePartsException.ThrowIfHasParts(middleName);
        NamePartsException.ThrowIfHasParts(lastName);

        FirstName = firstName ?? DefaultFirstName;
        MiddleName = middleName ?? DefaultMiddleName;
        LastName = lastName ?? DefaultLastName;
    }
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }
}
