using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using SchoolClubs.Application.Services.GradebookImport;

namespace SchoolClubs.Infrastructure.Spreadsheets;

/// <summary>
/// Трафарет журнала на ClosedXML. Лист «Журнал»: колонка A и строка 2 скрыты и несут id учеников и задач,
/// B — ученик, дальше по колонке на задачу. Скрытый (veryHidden) лист <c>_meta</c> и лист «Легенда».
/// </summary>
public partial class ClosedXmlGradebookSpreadsheet : IGradebookSpreadsheet
{
    public const string SheetName = "Журнал";
    public const string MetaSheetName = "_meta";
    public const string LegendSheetName = "Легенда";

    /// <summary>Маркер строки с id задач (A2).</summary>
    public const string IdsRowMarker = "#ids";

    private const int IdColumn = 1;
    private const int NameColumn = 2;
    private const int FirstTaskColumn = 3;
    private const int HeaderRow = 1;
    private const int IdsRow = 2;
    private const int FirstStudentRow = 3;

    private static readonly (string Symbol, string Meaning, string Synonyms, string Color)[] Legend =
    [
        (GradebookSymbols.Solved, "Решено", "1, да, ✓, v", "#C6EFCE"),
        (GradebookSymbols.SolvedWithRemarks, "Решено с замечаниями", "+/-, ±", "#E2F0B6"),
        (GradebookSymbols.UnsolvedWithRemarks, "Не решено, но с продвижением", "-/+", "#FCE4C4"),
        (GradebookSymbols.Unsolved, "Не решено", "0, нет", "#F8CBAD"),
    ];

    public byte[] Generate(GradebookTemplate template)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(SheetName);
        var lastTaskColumn = FirstTaskColumn + template.Tasks.Count - 1;
        var lastRow = FirstStudentRow + Math.Max(template.Students.Count, 1) - 1;

        sheet.Cell(HeaderRow, IdColumn).Value = "id";
        sheet.Cell(HeaderRow, NameColumn).Value = "Ученик";
        sheet.Cell(IdsRow, IdColumn).Value = IdsRowMarker;
        for (var i = 0; i < template.Tasks.Count; i++)
        {
            var task = template.Tasks[i];
            var column = FirstTaskColumn + i;
            sheet.Cell(HeaderRow, column).Value = task.Title is null
                ? $"Задача {task.Order}"
                : $"Задача {task.Order}: {task.Title}";
            if (task.Id is { } taskId)
                sheet.Cell(IdsRow, column).Value = taskId.ToString();
        }

        var entries = template.Entries.ToDictionary(e => (e.StudentId, e.TaskId), e => e.Status);
        for (var i = 0; i < template.Students.Count; i++)
        {
            var student = template.Students[i];
            var row = FirstStudentRow + i;
            sheet.Cell(row, IdColumn).Value = student.Id.ToString();
            sheet.Cell(row, NameColumn).Value = student.Name;
            for (var t = 0; t < template.Tasks.Count; t++)
            {
                if (template.Tasks[t].Id is { } taskId && entries.TryGetValue((student.Id, taskId), out var status))
                    sheet.Cell(row, FirstTaskColumn + t).Value = GradebookSymbols.ToSymbol(status);
            }
        }

        var header = sheet.Range(HeaderRow, NameColumn, HeaderRow, Math.Max(lastTaskColumn, NameColumn));
        header.Style.Font.Bold = true;
        header.Style.Alignment.WrapText = true;
        sheet.Column(NameColumn).Width = 32;
        sheet.Column(IdColumn).Hide();
        sheet.Row(IdsRow).Hide();
        sheet.SheetView.Freeze(IdsRow, NameColumn);

        if (template.Tasks.Count > 0)
        {
            sheet.Columns(FirstTaskColumn, lastTaskColumn).Width = 14;
            var marks = sheet.Range(FirstStudentRow, FirstTaskColumn, lastRow, lastTaskColumn);
            // Текстовый формат: иначе Excel принимает «+» за начало формулы, а «1» — за число.
            marks.Style.NumberFormat.Format = "@";
            marks.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            marks.Style.Protection.Locked = false;

            // Список-подсказка, но без запрета: синонимы вроде «1» и «да» тоже принимаются.
            var validation = marks.CreateDataValidation();
            validation.List($"\"{string.Join(",", GradebookSymbols.All)}\"", true);
            validation.IgnoreBlanks = true;
            validation.ShowErrorMessage = false;

            foreach (var (symbol, _, _, color) in Legend)
                marks.AddConditionalFormat().WhenEquals($"\"{symbol}\"").Fill.SetBackgroundColor(XLColor.FromHtml(color));
        }

        // Шапка и ФИО защищены от случайных правок; пароля нет.
        sheet.Protect()
            .AllowElement(XLSheetProtectionElements.FormatColumns)
            .AllowElement(XLSheetProtectionElements.FormatRows);

        AddLegend(workbook, template);
        AddMeta(workbook, template);
        workbook.Worksheet(SheetName).SetTabActive();

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    public ParsedGradebook Parse(Stream content)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(content);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            throw new InvalidSpreadsheetException("Не удалось открыть файл как таблицу .xlsx.");
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault(w => w.Name == SheetName)
                        ?? workbook.Worksheets.FirstOrDefault(w => w.Visibility == XLWorksheetVisibility.Visible)
                        ?? throw new InvalidSpreadsheetException("В файле нет листа с журналом.");
            var meta = workbook.Worksheets.FirstOrDefault(w => w.Name == MetaSheetName) is { } metaSheet
                ? ParseMeta(metaSheet)
                : null;
            var (columns, rows) = ParseSheet(sheet);
            return new ParsedGradebook(meta, columns, rows);
        }
    }

    private static (IReadOnlyList<ParsedColumn>, IReadOnlyList<ParsedRow>) ParseSheet(IXLWorksheet sheet)
    {
        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

        var nameColumn = FindNameColumn(sheet, lastColumn);
        var hasIdColumn = nameColumn > IdColumn;

        var taskColumns = Enumerable.Range(nameColumn + 1, Math.Max(0, lastColumn - nameColumn)).ToList();
        var hasIdsRow = Text(sheet.Cell(IdsRow, IdColumn)) == IdsRowMarker
                        || taskColumns.Any(c => Guid.TryParse(Text(sheet.Cell(IdsRow, c)), out _));
        var firstDataRow = hasIdsRow ? IdsRow + 1 : HeaderRow + 1;

        var columns = new List<ParsedColumn>();
        foreach (var column in taskColumns)
        {
            var headerText = Text(sheet.Cell(HeaderRow, column));
            Guid? taskId = hasIdsRow && Guid.TryParse(Text(sheet.Cell(IdsRow, column)), out var id) ? id : null;
            if (headerText.Length == 0 && taskId is null)
                continue;

            var number = TaskNumber().Match(headerText) is { Success: true } match
                ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)
                : (int?) null;
            columns.Add(new ParsedColumn(sheet.Column(column).ColumnLetter(), taskId, number, headerText));
        }

        if (columns.Count > IGradebookSpreadsheet.MaxTasks)
            throw new InvalidSpreadsheetException($"В файле больше {IGradebookSpreadsheet.MaxTasks} задач.");

        var columnNumbers = columns.ToDictionary(c => c.Letter, c => XLHelper.GetColumnNumberFromLetter(c.Letter));
        var rows = new List<ParsedRow>();
        for (var row = firstDataRow; row <= lastRow; row++)
        {
            Guid? studentId = hasIdColumn && Guid.TryParse(Text(sheet.Cell(row, IdColumn)), out var id) ? id : null;
            var name = Text(sheet.Cell(row, nameColumn));
            var cells = columns
                .Select(c => (c.Letter, Cell: sheet.Cell(row, columnNumbers[c.Letter])))
                .Select(c => new ParsedCell(c.Letter, c.Cell.Address.ToString()!, Text(c.Cell)))
                .Where(c => c.Text.Length > 0)
                .ToList();
            if (studentId is null && name.Length == 0 && cells.Count == 0)
                continue;

            rows.Add(new ParsedRow(row, studentId, name, cells));
            if (rows.Count > IGradebookSpreadsheet.MaxRows)
                throw new InvalidSpreadsheetException($"В файле больше {IGradebookSpreadsheet.MaxRows} строк.");
        }

        return (columns, rows);
    }

    /// <summary>
    /// В нашем трафарете ученик — колонка B (A скрыта); в «чужом» файле ищем заголовок «Ученик» / «ФИО»,
    /// иначе — первая колонка.
    /// </summary>
    private static int FindNameColumn(IXLWorksheet sheet, int lastColumn)
    {
        for (var column = 1; column <= Math.Min(lastColumn, 5); column++)
        {
            var header = Text(sheet.Cell(HeaderRow, column)).ToLowerInvariant();
            if (header is "ученик" or "ученики" or "фио" or "фамилия имя" or "ф.и.о.")
                return column;
        }

        return Text(sheet.Cell(HeaderRow, IdColumn)) == "id" ? NameColumn : IdColumn;
    }

    private static ParsedMeta? ParseMeta(IXLWorksheet sheet)
    {
        var values = new Dictionary<string, string>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        for (var row = 1; row <= lastRow; row++)
            values[Text(sheet.Cell(row, 1))] = Text(sheet.Cell(row, 2));

        if (!values.TryGetValue("format_version", out var version)
            || !int.TryParse(version, CultureInfo.InvariantCulture, out var formatVersion))
            return null;

        return new ParsedMeta(formatVersion,
            Guid.TryParse(values.GetValueOrDefault("lesson_id"), out var lessonId) ? lessonId : null,
            Guid.TryParse(values.GetValueOrDefault("group_id"), out var groupId) ? groupId : null,
            DateTimeOffset.TryParse(values.GetValueOrDefault("generated_at"), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var generatedAt)
                ? generatedAt
                : null);
    }

    private static void AddLegend(XLWorkbook workbook, GradebookTemplate template)
    {
        var legend = workbook.AddWorksheet(LegendSheetName);
        legend.Cell(1, 1).Value = "Группа";
        legend.Cell(1, 2).Value = template.GroupTitle;
        legend.Cell(2, 1).Value = "Занятие";
        legend.Cell(2, 2).Value = template.Date is { } date
            ? $"{date:dd.MM.yyyy}{(template.Topic is null ? "" : $" — {template.Topic}")}"
            : "любое занятие группы";

        legend.Cell(4, 1).Value = "Символ";
        legend.Cell(4, 2).Value = "Значение";
        legend.Cell(4, 3).Value = "Можно вводить и так";
        legend.Range(4, 1, 4, 3).Style.Font.Bold = true;
        for (var i = 0; i < Legend.Length; i++)
        {
            var (symbol, meaning, synonyms, color) = Legend[i];
            legend.Cell(5 + i, 1).Value = symbol;
            legend.Cell(5 + i, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(color);
            legend.Cell(5 + i, 2).Value = meaning;
            legend.Cell(5 + i, 3).Value = synonyms;
        }

        legend.Cell(10, 1).Value = "Пустая ячейка не меняет отметку в системе; очистить отметку можно только в сетке.";
        legend.Columns(1, 3).AdjustToContents();
    }

    private static void AddMeta(XLWorkbook workbook, GradebookTemplate template)
    {
        var meta = workbook.AddWorksheet(MetaSheetName);
        meta.Cell(1, 1).Value = "format_version";
        meta.Cell(1, 2).Value = IGradebookSpreadsheet.FormatVersion.ToString(CultureInfo.InvariantCulture);
        meta.Cell(2, 1).Value = "lesson_id";
        meta.Cell(2, 2).Value = template.LessonId?.ToString() ?? "";
        meta.Cell(3, 1).Value = "group_id";
        meta.Cell(3, 2).Value = template.GroupId.ToString();
        meta.Cell(4, 1).Value = "generated_at";
        meta.Cell(4, 2).Value = template.GeneratedAt.ToString("O", CultureInfo.InvariantCulture);
        meta.Visibility = XLWorksheetVisibility.VeryHidden;
    }

    private static string Text(IXLCell cell)
    {
        return cell.GetFormattedString().Trim();
    }

    /// <summary>«Задача 2», «Задача 2: Рекурсия», «№2», «2».</summary>
    [GeneratedRegex(@"^\s*(?:задача|task|№|#)?\s*(\d{1,3})(?!\d)", RegexOptions.IgnoreCase)]
    private static partial Regex TaskNumber();
}
