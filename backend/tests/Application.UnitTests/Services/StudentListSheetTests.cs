using NUnit.Framework;
using Shouldly;
using SchoolClubs.Application.Services.Admin;

namespace SchoolClubs.Application.UnitTests.Services;

[TestFixture]
[TestOf(typeof(StudentListSheet))]
public class StudentListSheetTests
{
    [Test]
    public void ToText_SkipsHeaderAndNumberColumn_KeepsRowNumbers()
    {
        var text = StudentListSheet.ToText([
            ["№", "Фамилия", "Имя", "Отчество", "Класс"],
            ["1", "Иванов", "Пётр", "Сергеевич", "7Б"],
            [],
            ["2.", "Петрова", "Анна"],
        ]);

        var lines = StudentNameParser.Parse(text);

        lines.Count.ShouldBe(2);
        lines[0].Number.ShouldBe(2);
        lines[0].Student.ShouldBe(new NewStudentRequest("Иванов", "Пётр", "Сергеевич", "7Б"));
        lines[1].Number.ShouldBe(4);
        lines[1].Student.ShouldBe(new NewStudentRequest("Петрова", "Анна", null, null));
    }

    [Test]
    public void ToText_FullNameInOneCell_WithoutHeader()
    {
        var text = StudentListSheet.ToText([["Иванов Пётр Сергеевич", "11а"], ["Сидоров"]]);

        var lines = StudentNameParser.Parse(text);

        lines[0].Student.ShouldBe(new NewStudentRequest("Иванов", "Пётр", "Сергеевич", "11а"));
        lines[1].Error.ShouldNotBeNull();
    }
}
