using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using JetBrains.Annotations;
using SchoolClubs.Domain.Common.Exceptions;
using SchoolClubs.Domain.Entities.Data;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Domain.Entities.Users;

/// <summary>
/// Обычный пользователь отличается от школьника реальным доступом к сервису.
/// Что он может делать, определяется набором ролей <see cref="Roles"/>.
/// </summary>
public class User : Profile
{
    private readonly List<UserRoleGrant> _roles = [];
    private readonly List<Student> _children = [];
    private readonly List<Group> _linkedGroups = [];

    public User(string? firstName = null,
        string? middleName = null,
        string? lastName = null,
        string email = NoEmail,
        string? phone = null,
        IList<UserRole>? roles = null) : base(firstName, middleName, lastName)
    {
        UniqueListElementsException.ThrowIfHasDuplicates(roles);

        ChangeEmail(email);
        ChangePhone(phone);
        foreach (var role in roles ?? [])
            GrantRole(role);
    }

    [UsedImplicitly]
    protected User()
    {
    }

    /// <summary>Заглушка для аккаунтов без почты (вход через провайдер, не отдавший подтверждённый адрес).</summary>
    public const string NoEmail = "invalid@school218.ru";

    public MailAddress Email { get; private set; } = null!;

    public bool HasEmail
    {
        get { return Email.Address != NoEmail; }
    }
    public string? Phone { get; private set; }

    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 32;

    /// <summary>Латиница, цифры, <c>_</c>, <c>.</c>, <c>-</c>; начинается с буквы или цифры.</summary>
    public const string UsernamePattern = "^[a-z0-9][a-z0-9_.-]*$";

    /// <summary>Необязательный уникальный ник; хранится в нижнем регистре, без ведущего <c>@</c>.</summary>
    public string? Username { get; private set; }

    /// <summary>Аватарка: файл в S3, метаданные — <see cref="DataEntry"/>.</summary>
    public Guid? AvatarId { get; private set; }

    public DataEntry? Avatar { get; private set; }

    public IReadOnlyCollection<UserRole> Roles
    {
        get { return _roles.Select(r => r.Role).ToList(); }
    }

    /// <summary>Дети, к отметкам которых есть доступ. Требует роль <see cref="UserRole.Parent"/>.</summary>
    public IReadOnlyCollection<Student> Children
    {
        get { return _children; }
    }

    /// <summary>Группы, которые ведёт учитель. Изменяется через <see cref="Group.AddTeacher"/>.</summary>
    public IReadOnlyCollection<Group> LinkedGroups
    {
        get { return _linkedGroups; }
    }

    /// <summary>
    /// Меняется при сбросе пароля и блокировке — все выданные сессии завершаются.
    /// Смена ролей штамп не трогает: сессии перевыпускаются с новыми ролями без разлогина.
    /// </summary>
    public string SecurityStamp { get; private set; } = NewSecurityStamp();

    public bool IsBlocked { get; private set; }

    /// <summary>Согласие на обработку персональных данных (152-ФЗ).</summary>
    public DateTimeOffset? ConsentAcceptedAt { get; private set; }

    public string? ConsentVersion { get; private set; }
    public string? ConsentIp { get; private set; }

    /// <summary>Email хранится в нижнем регистре: по нему ищут аккаунт при входе и регистрации.</summary>
    public void ChangeEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        email = email.Trim().ToLowerInvariant();
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

    /// <param name="username"><c>null</c> или пустая строка — убрать ник.</param>
    /// <remarks>Уникальность проверяет вызывающий: в домене нет доступа к другим пользователям.</remarks>
    public void ChangeUsername(string? username)
    {
        var normalized = NormalizeUsername(username);
        if (!string.IsNullOrWhiteSpace(username) && !IsValidUsername(normalized))
            throw new ArgumentException("invalid username passed", nameof(username));

        Username = normalized;
    }

    /// <summary>Обрезает пробелы и ведущий <c>@</c>, приводит к нижнему регистру; пустое — <c>null</c>.</summary>
    public static string? NormalizeUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        var normalized = username.Trim().TrimStart('@').ToLowerInvariant();
        return normalized.Length == 0 ? null : normalized;
    }

    /// <param name="normalized">Результат <see cref="NormalizeUsername"/>.</param>
    public static bool IsValidUsername(string? normalized)
    {
        return normalized is { Length: >= UsernameMinLength and <= UsernameMaxLength }
               && System.Text.RegularExpressions.Regex.IsMatch(normalized, UsernamePattern);
    }

    /// <param name="avatar"><c>null</c> — убрать аватарку.</param>
    /// <returns>Прежняя аватарка (её файл удаляет вызывающий) или <c>null</c>.</returns>
    /// <remarks>Прежняя аватарка известна, только если навигация <see cref="Avatar"/> загружена.</remarks>
    public DataEntry? ChangeAvatar(DataEntry? avatar)
    {
        var previous = Avatar;
        Avatar = avatar;
        AvatarId = avatar?.Id;
        return previous;
    }

    public bool HasRole(UserRole role)
    {
        return _roles.Any(r => r.Role == role);
    }

    /// <returns><c>false</c>, если роль уже выдана.</returns>
    public bool GrantRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role), role, "unknown role");
        if (HasRole(role))
            return false;

        _roles.Add(new UserRoleGrant(role));
        return true;
    }

    /// <returns><c>false</c>, если роли не было.</returns>
    /// <exception cref="UserRoleException">У учителя остались группы или у родителя — дети.</exception>
    public bool RevokeRole(UserRole role)
    {
        if (!HasRole(role))
            return false;
        if (role == UserRole.Teacher && _linkedGroups.Count > 0)
            throw new UserRoleException("teacher role can't be revoked while user has linked groups");
        if (role == UserRole.Parent && _children.Count > 0)
            throw new UserRoleException("parent role can't be revoked while user has children");

        _roles.RemoveAll(r => r.Role == role);
        return true;
    }

    /// <returns><c>false</c>, если ребёнок уже привязан.</returns>
    public bool AddChild(Student child)
    {
        ArgumentNullException.ThrowIfNull(child);
        UserRoleException.ThrowIfMissing(this, UserRole.Parent);
        if (_children.Any(c => c.Id == child.Id))
            return false;

        _children.Add(child);
        return true;
    }

    /// <returns><c>false</c>, если ребёнок не был привязан.</returns>
    public bool RemoveChild(Student child)
    {
        ArgumentNullException.ThrowIfNull(child);
        return _children.RemoveAll(c => c.Id == child.Id) > 0;
    }

    public void Block()
    {
        if (IsBlocked)
            return;

        IsBlocked = true;
        RotateSecurityStamp();
    }

    public void Unblock()
    {
        if (!IsBlocked)
            return;

        IsBlocked = false;
    }

    public void AcceptConsent(string version, DateTimeOffset acceptedAt, string? ip = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        ConsentVersion = version.Trim();
        ConsentAcceptedAt = acceptedAt;
        ConsentIp = string.IsNullOrWhiteSpace(ip) ? null : ip.Trim();
    }

    /// <returns><c>true</c>, если принята именно текущая версия политики.</returns>
    public bool HasAcceptedConsent(string currentVersion)
    {
        return ConsentAcceptedAt is not null && ConsentVersion == currentVersion;
    }

    /// <summary>Инвалидирует выданные сессии (например, после сброса пароля).</summary>
    public void RotateSecurityStamp()
    {
        SecurityStamp = NewSecurityStamp();
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

    private static string NewSecurityStamp()
    {
        return Guid.NewGuid().ToString("N");
    }
}
