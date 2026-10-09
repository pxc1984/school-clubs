using NUnit.Framework;
using Shouldly;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Domain.UnitTests.Entities;

[TestFixture]
[TestOf(typeof(TaskNumber))]
public class TaskNumberTests
{
    [Test]
    public void Comparer_IsNatural()
    {
        string[] numbers = ["10", "2", "1.10", "1.2", "1", "2б", "2а", "Д1", "1.1", "01.3"];

        numbers.Order(TaskNumber.Comparer).ShouldBe(["1", "1.1", "1.2", "01.3", "1.10", "2", "2а", "2б", "10", "Д1"]);
    }

    [TestCase(new string[0], "1")]
    [TestCase(new[] { "1", "2" }, "3")]
    [TestCase(new[] { "1.1", "1.2" }, "2")]
    [TestCase(new[] { "Д1", "Бонус" }, "1")]
    public void Next_FollowsLeadingNumbers(string[] numbers, string expected)
    {
        TaskNumber.Next(numbers).ShouldBe(expected);
    }

    [Test]
    public void Normalize_Trims()
    {
        TaskNumber.Normalize(" 1.1 ").ShouldBe("1.1");
        Should.Throw<ArgumentException>(() => TaskNumber.Normalize("  "));
    }
}
