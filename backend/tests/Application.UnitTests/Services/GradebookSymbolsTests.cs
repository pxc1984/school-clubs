using NUnit.Framework;
using Shouldly;
using SchoolClubs.Application.Services.GradebookImport;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Application.UnitTests.Services;

[TestFixture]
[TestOf(typeof(GradebookSymbols))]
public class GradebookSymbolsTests
{
    [TestCase("+", GradebookEntryTaskStatus.Solved)]
    [TestCase(" 1 ", GradebookEntryTaskStatus.Solved)]
    [TestCase("ДА", GradebookEntryTaskStatus.Solved)]
    [TestCase("✓", GradebookEntryTaskStatus.Solved)]
    [TestCase("V", GradebookEntryTaskStatus.Solved)]
    [TestCase("+.", GradebookEntryTaskStatus.SolvedWithRemarks)]
    [TestCase("+ /-", GradebookEntryTaskStatus.SolvedWithRemarks)]
    [TestCase("±", GradebookEntryTaskStatus.SolvedWithRemarks)]
    [TestCase("-.", GradebookEntryTaskStatus.UnsolvedWithRemarks)]
    [TestCase("−.", GradebookEntryTaskStatus.UnsolvedWithRemarks)]
    [TestCase("-/+", GradebookEntryTaskStatus.UnsolvedWithRemarks)]
    [TestCase("-", GradebookEntryTaskStatus.Unsolved)]
    [TestCase("0", GradebookEntryTaskStatus.Unsolved)]
    [TestCase("Нет", GradebookEntryTaskStatus.Unsolved)]
    public void TryParse_Synonyms(string text, GradebookEntryTaskStatus expected)
    {
        GradebookSymbols.TryParse(text, out var status).ShouldBeTrue();
        status.ShouldBe(expected);
    }

    [TestCase("решил почти")]
    [TestCase("2")]
    [TestCase("++")]
    public void TryParse_Garbage_Fails(string text)
    {
        GradebookSymbols.TryParse(text, out _).ShouldBeFalse();
    }

    [Test]
    public void ToSymbol_RoundTrips()
    {
        foreach (var status in Enum.GetValues<GradebookEntryTaskStatus>())
        {
            GradebookSymbols.TryParse(GradebookSymbols.ToSymbol(status), out var parsed).ShouldBeTrue();
            parsed.ShouldBe(status);
        }
    }
}
