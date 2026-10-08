namespace SchoolClubs.Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    /// <summary>Причина отказа для фронтенда.</summary>
    public const string ConsentRequired = "consent_required";

    public const string Blocked = "blocked";

    public ForbiddenAccessException() : base()
    {
    }

    /// <param name="code">Машиночитаемая причина, попадает в ProblemDetails как <c>code</c>.</param>
    public ForbiddenAccessException(string code) : base(code)
    {
        Code = code;
    }

    public string? Code { get; }
}
