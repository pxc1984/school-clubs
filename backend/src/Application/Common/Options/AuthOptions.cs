namespace SchoolClubs.Application.Common.Options;

/// <summary>Секция <c>Auth</c>.</summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Версия политики обработки ПДн. При смене версии все пользователи снова проходят через <c>/consent</c>.
    /// </summary>
    public string ConsentVersion { get; set; } = "1";

    /// <summary>
    /// Пароль сервисного админа (<c>Auth__ServiceAdminPassword</c> в .env). Пустой — сервисный вход выключен.
    /// </summary>
    public string? ServiceAdminPassword { get; set; }
}
