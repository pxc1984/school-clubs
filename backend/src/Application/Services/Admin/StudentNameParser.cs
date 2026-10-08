using System.Text.RegularExpressions;

namespace SchoolClubs.Application.Services.Admin;

/// <summary>
/// Разбор списка учеников, вставленного столбиком (из Excel или документа):
/// «Фамилия Имя [Отчество] [класс]» на строку, части разделены пробелами или табуляцией.
/// </summary>
public static partial class StudentNameParser
{
    public const int MaxLines = 200;

    public record Line(int Number, string Source, NewStudentRequest? Student, string? Error);

    public static IReadOnlyList<Line> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return text.Split('\n')
            .Select((source, index) => (Source: source.Trim(), Number: index + 1))
            .Where(l => l.Source.Length > 0)
            .Take(MaxLines)
            .Select(l => ParseLine(l.Number, l.Source))
            .ToList();
    }

    private static Line ParseLine(int number, string source)
    {
        var parts = source.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries).ToList();

        string? grade = null;
        if (parts.Count > 2 && GradePattern().IsMatch(parts[^1]))
        {
            grade = parts[^1];
            parts.RemoveAt(parts.Count - 1);
        }

        if (parts.Count is < 2 or > 3)
            return new Line(number, source, null, "Ожидается «Фамилия Имя [Отчество] [класс]».");

        return new Line(number, source,
            new NewStudentRequest(parts[0], parts[1], parts.Count == 3 ? parts[2] : null, grade), null);
    }

    /// <summary>Класс: «7», «7Б», «11а».</summary>
    [GeneratedRegex("^[0-9]{1,2}[А-ЯЁа-яёA-Za-z]?$")]
    private static partial Regex GradePattern();
}
