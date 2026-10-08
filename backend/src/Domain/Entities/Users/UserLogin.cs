using JetBrains.Annotations;
using SchoolClubs.Domain.Common;

namespace SchoolClubs.Domain.Entities.Users;

public enum LoginProvider
{
    Password,
    Yandex,
}

/// <summary>
/// Способ входа пользователя. Хранится отдельно от <see cref="User"/>, чтобы не смешивать профиль и секреты;
/// у одного пользователя может быть и пароль, и Яндекс ID.
/// </summary>
public class UserLogin : IAuditableEntity
{
    private UserLogin(Guid userId, LoginProvider provider, string providerKey, string? passwordHash)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("user id is empty", nameof(userId));

        UserId = userId;
        Provider = provider;
        ProviderKey = NormalizeKey(provider, providerKey);
        PasswordHash = passwordHash;
    }

    [UsedImplicitly]
    private UserLogin()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public LoginProvider Provider { get; private set; }

    /// <summary>Нормализованный email для пароля или <c>id</c> пользователя Яндекса.</summary>
    public string ProviderKey { get; private set; } = string.Empty;

    public string? PasswordHash { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }

    /// <param name="passwordHash">Уже посчитанный хеш; хеширование — забота Application.</param>
    public static UserLogin ForPassword(User user, string email, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new UserLogin(user.Id, LoginProvider.Password, email, passwordHash);
    }

    public static UserLogin ForYandex(User user, string yandexId)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserLogin(user.Id, LoginProvider.Yandex, yandexId, null);
    }

    /// <summary>Приводит ключ к виду, в котором он хранится и ищется.</summary>
    public static string NormalizeKey(LoginProvider provider, string providerKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);

        var key = providerKey.Trim();
        return provider == LoginProvider.Password ? key.ToLowerInvariant() : key;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        if (Provider != LoginProvider.Password)
            throw new InvalidOperationException($"{Provider} login has no password");

        PasswordHash = passwordHash;
    }

    public void MarkUsed(DateTimeOffset usedAt)
    {
        LastUsedAt = usedAt;
    }
}
