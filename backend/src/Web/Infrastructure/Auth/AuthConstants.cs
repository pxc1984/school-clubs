namespace SchoolClubs.Web.Infrastructure.Auth;

public static class AuthSchemes
{
    /// <summary>Основная HttpOnly-cookie сессии.</summary>
    public const string Session = "session";

    /// <summary>Временная cookie между callback внешнего провайдера и выпуском сессии.</summary>
    public const string External = "external";

    /// <summary>Префикс схем OIDC-провайдеров: <c>oidc-yandex</c>.</summary>
    public const string OidcPrefix = "oidc-";
}

public static class SessionClaims
{
    public const string Subject = "sub";
    public const string Role = "role";
    public const string SecurityStamp = "stamp";

    /// <summary>Есть, пока не принята текущая версия политики ПДн.</summary>
    public const string ConsentRequired = "consent_required";

    /// <summary>Сервисный админ (вход по паролю из конфига, без аккаунта).</summary>
    public const string ServiceAdmin = "svc";
}

public static class Policies
{
    /// <summary>Любая сессия, даже без принятого согласия.</summary>
    public const string Session = "session";

    /// <summary>Пользователь с аккаунтом (не сервисный админ).</summary>
    public const string User = "user";

    /// <summary>Принята текущая версия политики ПДн.</summary>
    public const string Consent = "consent";

    public const string Parent = "parent";
    public const string Teacher = "teacher";
    public const string Admin = "admin";
}

public static class RateLimits
{
    public const string ServiceAdminLogin = "service-admin-login";
}
