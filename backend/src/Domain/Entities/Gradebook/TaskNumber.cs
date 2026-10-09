using System.Globalization;

namespace SchoolClubs.Domain.Entities.Gradebook;

/// <summary>
/// Номер задачи в свободной форме («1», «1.1», «2а», «Д1») и «естественный» порядок:
/// числа сравниваются как числа, поэтому 1 &lt; 1.1 &lt; 1.2 &lt; 2 &lt; 10.
/// </summary>
public static class TaskNumber
{
    public const int MaxLength = 20;

    /// <summary>Пробелы и двоеточие заняты заголовком столбца трафарета: «Задача 1.1: Название».</summary>
    public static bool IsValid(string? number)
    {
        return !string.IsNullOrEmpty(number)
               && number.Length <= MaxLength
               && !number.Any(c => char.IsWhiteSpace(c) || c == ':');
    }

    /// <exception cref="ArgumentException">Пустой, длинный или с пробелами/двоеточием.</exception>
    public static string Normalize(string? number)
    {
        var trimmed = number?.Trim();
        if (!IsValid(trimmed))
            throw new ArgumentException(
                $"task number should be 1-{MaxLength} characters without spaces and colons", nameof(number));
        return trimmed!;
    }

    /// <summary>Номера совпадают без учёта регистра: «2А» и «2а» — одна задача.</summary>
    public static bool AreSame(string a, string b)
    {
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Следующий номер после ведущих чисел: после «1.1», «1.2» — «2».</summary>
    public static string Next(IEnumerable<string> numbers)
    {
        var max = numbers
            .Select(n => new string(n.TakeWhile(char.IsAsciiDigit).Take(9).ToArray()))
            .Where(d => d.Length > 0)
            .Select(d => long.Parse(d, CultureInfo.InvariantCulture))
            .DefaultIfEmpty(0)
            .Max();
        return (max + 1).ToString(CultureInfo.InvariantCulture);
    }

    public static IComparer<string> Comparer { get; } = new NaturalComparer();

    private sealed class NaturalComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            int i = 0, j = 0;
            while (i < x.Length && j < y.Length)
            {
                if (char.IsAsciiDigit(x[i]) && char.IsAsciiDigit(y[j]))
                {
                    var xs = i;
                    var ys = j;
                    while (i < x.Length && char.IsAsciiDigit(x[i])) i++;
                    while (j < y.Length && char.IsAsciiDigit(y[j])) j++;
                    var xd = x.AsSpan(xs, i - xs).TrimStart('0');
                    var yd = y.AsSpan(ys, j - ys).TrimStart('0');
                    var byNumber = xd.Length != yd.Length
                        ? xd.Length.CompareTo(yd.Length)
                        : xd.CompareTo(yd, StringComparison.Ordinal);
                    if (byNumber != 0) return byNumber;
                    continue;
                }

                var byChar = string.Compare(x, i, y, j, 1, CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
                if (byChar != 0) return byChar;
                i++;
                j++;
            }

            var byLength = (x.Length - i).CompareTo(y.Length - j);
            return byLength != 0 ? byLength : string.Compare(x, y, StringComparison.Ordinal);
        }
    }
}
