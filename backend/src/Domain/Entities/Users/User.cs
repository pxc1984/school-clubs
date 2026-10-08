using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using JetBrains.Annotations;
using SchoolClubs.Domain.Common.Exceptions;

namespace SchoolClubs.Domain.Entities.Users;

/// <summary>
/// Обычный пользователь отличается от школьника реальным доступом к сервису
/// </summary>
public class User : Profile
{
    public User(string? firstName = null,
        string? middleName = null,
        string? lastName = null,
        string email = "invalid@school218.ru",
        string? phone = null) : base(firstName, middleName, lastName)
    {
        ChangeEmail(email);
        ChangePhone(phone);
    }

    [UsedImplicitly]
    protected User()
    {
    }

    public MailAddress Email { get; private set; } = null!;
    public string? Phone { get; private set; }

    public void ChangeEmail(string email)
    {
        NamePartsException.ThrowIfHasParts(email);

        if (!MailAddress.TryCreate(email, out var emailAddress))
            throw new ArgumentException("invalid email address passed", nameof(email));

        Email = emailAddress;
    }

    public void ChangePhone(string? phone)
    {
        if (!new PhoneAttribute().IsValid(phone))
            throw new ArgumentException("invalid phone number passed", nameof(phone));

        Phone = phone;
    }
}
