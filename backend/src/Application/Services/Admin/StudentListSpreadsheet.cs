using System.Text.RegularExpressions;
using SchoolClubs.Application.Services.GradebookImport;

namespace SchoolClubs.Application.Services.Admin;

/// <summary>Чтение списка учеников из XLSX. Реализация на ClosedXML — в Infrastructure.</summary>
public interface IStudentListSpreadsheet
{
    public const int MaxRows = 1000;

    /// <summary>Первый лист: на строку листа — тексты её непустых ячеек слева направо; пустая строка — пустой список.</summary>
    /// <exception cref="InvalidSpreadsheetException">Не xlsx, нет листов или больше <see cref="MaxRows"/> строк.</exception>
    IReadOnlyList<IReadOnlyList<string>> ReadRows(Stream content);
}

/// <summary>
/// Строки листа → текст для <see cref="StudentNameParser"/>: ФИО одной ячейкой или по колонкам
/// (Фамилия | Имя | Отчество | Класс), без строки заголовка и колонки «№». Номер строки текста = номер строки листа.
/// </summary>
public static partial class StudentListSheet
{
    private static readonly string[] HeaderWords = ["фамили", "фио", "имя", "ученик", "класс"];

    public static string ToText(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var headerSkipped = false;
        var lines = new List<string>(rows.Count);
        foreach (var row in rows)
        {
            var cells = row.Select(c => c.Trim()).Where(c => c.Length > 0).ToList();
            if (!headerSkipped && cells.Count > 0)
            {
                headerSkipped = true;
                if (IsHeader(cells))
                {
                    lines.Add("");
                    continue;
                }
            }

            if (cells.Count >= 2 && RowNumberPattern().IsMatch(cells[0]))
                cells.RemoveAt(0);
            lines.Add(string.Join(' ', cells));
        }

        return string.Join('\n', lines);
    }

    private static bool IsHeader(IEnumerable<string> cells)
    {
        return cells.Any(c => HeaderWords.Any(w => c.Contains(w, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Порядковый номер в колонке «№»: «1», «12.».</summary>
    [GeneratedRegex(@"^[0-9]{1,4}\.?$")]
    private static partial Regex RowNumberPattern();
}
