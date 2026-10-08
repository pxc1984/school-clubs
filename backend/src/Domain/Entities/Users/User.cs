using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using SchoolClubs.Domain.Common;
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
        NamePartsException.ThrowIfHasParts(email);

        if (!MailAddress.TryCreate(email, out var emailAddress))
            throw new ArgumentException("invalid email address passed");
        if (!new PhoneAttribute().IsValid(phone)) throw new ArgumentException("invalid phone number passed");

        Email = emailAddress;
        Phone = phone;
    }

    public MailAddress Email { get; set; }
    public string? Phone { get; set; }
}
