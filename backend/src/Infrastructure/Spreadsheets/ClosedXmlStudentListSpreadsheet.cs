using ClosedXML.Excel;
using SchoolClubs.Application.Services.Admin;
using SchoolClubs.Application.Services.GradebookImport;

namespace SchoolClubs.Infrastructure.Spreadsheets;

public class ClosedXmlStudentListSpreadsheet : IStudentListSpreadsheet
{
    public IReadOnlyList<IReadOnlyList<string>> ReadRows(Stream content)
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
            var sheet = workbook.Worksheets.FirstOrDefault()
                        ?? throw new InvalidSpreadsheetException("В файле нет листов.");
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow > IStudentListSpreadsheet.MaxRows)
                throw new InvalidSpreadsheetException($"В файле больше {IStudentListSpreadsheet.MaxRows} строк.");

            var rows = new List<IReadOnlyList<string>>(lastRow);
            for (var row = 1; row <= lastRow; row++)
            {
                rows.Add(sheet.Row(row).CellsUsed()
                    .Select(c => c.GetFormattedString().Trim())
                    .Where(t => t.Length > 0)
                    .ToList());
            }

            return rows;
        }
    }
}
