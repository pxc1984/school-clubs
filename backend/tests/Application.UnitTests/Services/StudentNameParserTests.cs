using NUnit.Framework;
using Shouldly;
using SchoolClubs.Application.Services.Admin;

namespace SchoolClubs.Application.UnitTests.Services;

[TestFixture]
[TestOf(typeof(StudentNameParser))]
public class StudentNameParserTests
{
    [Test]
    public void Parse_ColumnWithTabsGradesAndBlankLines()
    {
        var lines = StudentNameParser.Parse("Иванов Пётр Сергеевич\n\n  Петрова\tАнна\t7Б \r\nСидоров\n");

        lines.Count.ShouldBe(3);

        lines[0].Number.ShouldBe(1);
        lines[0].Student.ShouldBe(new NewStudentRequest("Иванов", "Пётр", "Сергеевич", null));

        lines[1].Number.ShouldBe(3);
        lines[1].Student.ShouldBe(new NewStudentRequest("Петрова", "Анна", null, "7Б"));

        lines[2].Number.ShouldBe(4);
        lines[2].Student.ShouldBeNull();
        lines[2].Error.ShouldNotBeNull();
    }

    [Test]
    public void Parse_FullNameWithGrade()
    {
        var line = StudentNameParser.Parse("Иванов Пётр Сергеевич 11а").ShouldHaveSingleItem();

        line.Student.ShouldBe(new NewStudentRequest("Иванов", "Пётр", "Сергеевич", "11а"));
    }

    [Test]
    public void Parse_TooManyParts_IsError()
    {
        StudentNameParser.Parse("Иванов Пётр Сергеевич Лишний Ещё").ShouldHaveSingleItem().Error.ShouldNotBeNull();
    }
}
