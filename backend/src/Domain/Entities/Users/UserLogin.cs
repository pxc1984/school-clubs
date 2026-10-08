using System.Text.RegularExpressions;
using JetBrains.Annotations;
using SchoolClubs.Domain.Common;

namespace SchoolClubs.Domain.Entities.Users;

/// <summary>
/// Способ входа пользователя. Хранится отдельно от <see cref="User"/>, чтобы не смешивать профиль и секреты;
/// у одного пользователя может быть и пароль, и несколько внешних провайдеров (Яндекс ID и т.п.).
/// </summary>
public partial class UserLogin : IAuditableEntity
{
    /// <summary>Вход по email и паролю.</summary>
    public const string PasswordProvider = "password";

    public const int ProviderMaxLength = 32;

    private UserLogin(Guid userId, string provider, string providerKey, string? passwordHash)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("user id is empty", nameof(userId));

        UserId = userId;
        Provider = NormalizeProvider(provider);
        ProviderKey = NormalizeKey(Provider, providerKey);
        PasswordHash = passwordHash;
    }

    [UsedImplicitly]
    private UserLogin()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }

    /// <summary><see cref="PasswordProvider"/> или имя внешнего провайдера в нижнем регистре (<c>yandex</c>).</summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>Нормализованный email для пароля или <c>sub</c> пользователя у внешнего провайдера.</summary>
    public string ProviderKey { get; private set; } = string.Empty;

    public string? PasswordHash { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }

    public bool IsExternal
    {
        get { return Provider != PasswordProvider; }
    }

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }

    /// <param name="passwordHash">Уже посчитанный хеш; хеширование — забота Application.</param>
    public static UserLogin ForPassword(User user, string email, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new UserLogin(user.Id, PasswordProvider, email, passwordHash);
    }

    /// <param name="subject">Стабильный идентификатор пользователя у провайдера (claim <c>sub</c>).</param>
    public static UserLogin ForExternal(User user, string provider, string subject)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (NormalizeProvider(provider) == PasswordProvider)
            throw new ArgumentException("password provider isn't external", nameof(provider));

        return new UserLogin(user.Id, provider, subject, null);
    }

    public static string NormalizeProvider(string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        var normalized = provider.Trim().ToLowerInvariant();
        if (normalized.Length > ProviderMaxLength || !ProviderPattern().IsMatch(normalized))
            throw new ArgumentException($"invalid login provider '{provider}'", nameof(provider));

        return normalized;
    }

    /// <summary>Приводит ключ к виду, в котором он хранится и ищется.</summary>
    public static string NormalizeKey(string provider, string providerKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);

        var key = providerKey.Trim();
        return NormalizeProvider(provider) == PasswordProvider ? key.ToLowerInvariant() : key;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        if (IsExternal)
            throw new InvalidOperationException($"{Provider} login has no password");

        PasswordHash = passwordHash;
    }

    public void MarkUsed(DateTimeOffset usedAt)
    {
        LastUsedAt = usedAt;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]*$")]
    private static partial Regex ProviderPattern();
}
