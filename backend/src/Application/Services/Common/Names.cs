using System.Text.RegularExpressions;

namespace SchoolClubs.Application.Services.Common;

/// <summary>Сравнение ФИО, как его пишут люди: без учёта регистра, <c>ё</c>/<c>е</c> и лишних пробелов.</summary>
public static partial class Names
{
    public static string Normalize(string value)
    {
        return Whitespace().Replace(value.Trim().ToLowerInvariant().Replace('ё', 'е'), " ");
    }

    public static IReadOnlyList<string> Tokens(string name)
    {
        return Normalize(name).Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
