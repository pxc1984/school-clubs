using ClosedXML.Excel;
using SchoolClubs.Application.Services.GradebookImport;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Infrastructure.Spreadsheets;

namespace SchoolClubs.Infrastructure.IntegrationTests.Spreadsheets;

/// <summary>Константы, с которыми сгенерирована фикстура <c>libreoffice-roundtrip.xlsx</c>.</summary>
internal static class SpreadsheetFixtures
{
    public static readonly Guid Group = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Lesson = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Task1 = Guid.Parse("33333333-3333-3333-3333-333333333331");
    public static readonly Guid Task2 = Guid.Parse("33333333-3333-3333-3333-333333333332");
    public static readonly Guid Alice = Guid.Parse("44444444-4444-4444-4444-444444444441");
    public static readonly Guid Bob = Guid.Parse("44444444-4444-4444-4444-444444444442");

    public static GradebookTemplate Template()
    {
        return new GradebookTemplate(Group, "Робототехника", Lesson, new DateOnly(2026, 10, 1), "Датчики",
            [new TemplateTask(Task1, 1, null), new TemplateTask(Task2, 2, "Рекурсия")],
            [new TemplateStudent(Alice, "Андреева Алиса"), new TemplateStudent(Bob, "Борисов Борис")],
            [
                new TemplateEntry(Alice, Task1, GradebookEntryTaskStatus.Solved),
                new TemplateEntry(Bob, Task2, GradebookEntryTaskStatus.UnsolvedWithRemarks),
            ],
            new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
    }
}

[TestFixture]
[TestOf(typeof(ClosedXmlGradebookSpreadsheet))]
public class SpreadsheetTests
{
    private readonly ClosedXmlGradebookSpreadsheet _spreadsheet = new();

    private ParsedGradebook Parse(byte[] bytes)
    {
        return _spreadsheet.Parse(new MemoryStream(bytes));
    }

    /// <summary>Сгенерировать трафарет, поправить его как человек в Excel и сохранить.</summary>
    private byte[] Edit(Action<IXLWorksheet> edit)
    {
        using var workbook = new XLWorkbook(new MemoryStream(_spreadsheet.Generate(SpreadsheetFixtures.Template())));
        var sheet = workbook.Worksheet(ClosedXmlGradebookSpreadsheet.SheetName);
        sheet.Unprotect();
        edit(sheet);
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private static IReadOnlyList<(Guid? Id, string Name, string Cells)> Summary(ParsedGradebook parsed)
    {
        return parsed.Rows
            .Select(r => (r.StudentId, r.Name, string.Join(" ", r.Cells.Select(c => $"{c.Address}={c.Text}"))))
            .ToList();
    }

    [Test]
    public void Generate_ThenParse_RoundTrips()
    {
        var bytes = _spreadsheet.Generate(SpreadsheetFixtures.Template());

        var parsed = Parse(bytes);

        parsed.Meta.ShouldBe(new ParsedMeta(1, SpreadsheetFixtures.Lesson, SpreadsheetFixtures.Group,
            new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero)));
        parsed.Columns.ShouldBe(
        [
            new ParsedColumn("C", SpreadsheetFixtures.Task1, 1, "Задача 1"),
            new ParsedColumn("D", SpreadsheetFixtures.Task2, 2, "Задача 2: Рекурсия"),
        ]);
        Summary(parsed).ShouldBe(
        [
            (SpreadsheetFixtures.Alice, "Андреева Алиса", "C3=+"),
            (SpreadsheetFixtures.Bob, "Борисов Борис", "D4=-."),
        ]);
    }

    [Test]
    public void Generate_LayoutMatchesDesign()
    {
        using var workbook = new XLWorkbook(new MemoryStream(_spreadsheet.Generate(SpreadsheetFixtures.Template())));
        var sheet = workbook.Worksheet(ClosedXmlGradebookSpreadsheet.SheetName);

        workbook.Worksheet(ClosedXmlGradebookSpreadsheet.MetaSheetName).Visibility
            .ShouldBe(XLWorksheetVisibility.VeryHidden);
        workbook.Worksheets.Select(w => w.Name).ShouldContain(ClosedXmlGradebookSpreadsheet.LegendSheetName);
        sheet.Column(1).IsHidden.ShouldBeTrue();
        sheet.Row(2).IsHidden.ShouldBeTrue();
        sheet.IsProtected.ShouldBeTrue();
        sheet.Cell("B3").Style.Protection.Locked.ShouldBeTrue();
        sheet.Cell("C3").Style.Protection.Locked.ShouldBeFalse();
        sheet.SheetView.SplitRow.ShouldBe(2);
        sheet.SheetView.SplitColumn.ShouldBe(2);
        sheet.DataValidations.ShouldHaveSingleItem().Value.ShouldContain("+.");
        sheet.ConditionalFormats.Count().ShouldBe(4);
    }

    [Test]
    public void DeletedRow_IsSimplyAbsent()
    {
        var parsed = Parse(Edit(sheet => sheet.Row(3).Delete()));

        Summary(parsed).ShouldBe([(SpreadsheetFixtures.Bob, "Борисов Борис", "D3=-.")]);
    }

    [Test]
    public void ReorderedRows_KeepTheirIds()
    {
        var parsed = Parse(Edit(sheet =>
        {
            // Как сортировка по ФИО по убыванию: строки меняются местами целиком.
            var alice = sheet.Range("A3:D3").Cells().Select(c => c.GetString()).ToArray();
            var bob = sheet.Range("A4:D4").Cells().Select(c => c.GetString()).ToArray();
            for (var i = 0; i < 4; i++)
            {
                sheet.Cell(3, i + 1).Value = bob[i];
                sheet.Cell(4, i + 1).Value = alice[i];
            }
        }));

        Summary(parsed).ShouldBe(
        [
            (SpreadsheetFixtures.Bob, "Борисов Борис", "D3=-."),
            (SpreadsheetFixtures.Alice, "Андреева Алиса", "C4=+"),
        ]);
    }

    [Test]
    public void Synonyms_AreReadAsTyped_NumbersIncluded()
    {
        var parsed = Parse(Edit(sheet =>
        {
            sheet.Cell("D3").Value = "да";
            sheet.Cell("C4").Value = 1;
            sheet.Cell("D4").Value = " ± ";
        }));

        Summary(parsed).ShouldBe(
        [
            (SpreadsheetFixtures.Alice, "Андреева Алиса", "C3=+ D3=да"),
            (SpreadsheetFixtures.Bob, "Борисов Борис", "C4=1 D4=±"),
        ]);
    }

    [Test]
    public void ForeignFileWithoutMeta_IsReadByText()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Лист1");
        sheet.Cell("A1").Value = "ФИО";
        sheet.Cell("B1").Value = "Задача 1";
        sheet.Cell("C1").Value = "2";
        sheet.Cell("A2").Value = "Алиса Андреева";
        sheet.Cell("B2").Value = "+";
        sheet.Cell("A4").Value = "Борисов Борис";
        sheet.Cell("C4").Value = "нет";
        using var output = new MemoryStream();
        workbook.SaveAs(output);

        var parsed = Parse(output.ToArray());

        parsed.Meta.ShouldBeNull();
        parsed.Columns.ShouldBe([new ParsedColumn("B", null, 1, "Задача 1"), new ParsedColumn("C", null, 2, "2")]);
        Summary(parsed).ShouldBe([((Guid?) null, "Алиса Андреева", "B2=+"), (null, "Борисов Борис", "C4=нет")]);
    }

    [Test]
    public void LibreOfficeFile_IsParsed()
    {
        var bytes = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "Spreadsheets", "Fixtures", "libreoffice-roundtrip.xlsx"));

        var parsed = Parse(bytes);

        parsed.Meta!.LessonId.ShouldBe(SpreadsheetFixtures.Lesson);
        parsed.Columns.Select(c => c.TaskId).ShouldBe([SpreadsheetFixtures.Task1, SpreadsheetFixtures.Task2]);
        Summary(parsed).ShouldBe(
        [
            (SpreadsheetFixtures.Alice, "Андреева Алиса", "C3=+"),
            (SpreadsheetFixtures.Bob, "Борисов Борис", "D4=-."),
        ]);
    }

    [Test]
    public void NotAnXlsx_Throws()
    {
        Should.Throw<InvalidSpreadsheetException>(() => Parse("PK\u0003\u0004 not really a zip"u8.ToArray()));
    }

    [Test]
    public void TooManyTasks_Throws()
    {
        var bytes = Edit(sheet =>
        {
            for (var column = 5; column <= 3 + IGradebookSpreadsheet.MaxTasks; column++)
                sheet.Cell(1, column).Value = $"Задача {column - 2}";
        });

        Should.Throw<InvalidSpreadsheetException>(() => Parse(bytes));
    }
}
