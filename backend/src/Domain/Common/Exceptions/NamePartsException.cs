using System.Runtime.CompilerServices;

namespace SchoolClubs.Domain.Common.Exceptions;

public class NamePartsException(string what) : ArgumentException($"{what} shouldn't have parts", what)
{
    public static void ThrowIfHasParts(string? value, [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        if (value != null && value.Any(char.IsWhiteSpace))
            throw new NamePartsException(paramName);
    }
}
