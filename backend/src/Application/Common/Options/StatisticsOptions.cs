using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Application.Common.Options;

/// <summary>Секция <c>Statistics</c>: веса отметок можно подстраивать без релиза.</summary>
public class StatisticsOptions
{
    public const string SectionName = "Statistics";

    public StatisticsWeights Weights { get; set; } = new();
}

/// <summary>Вклад отметки в балл ученика, от 0 до 1. Задача без отметки у посетившего — 0.</summary>
public class StatisticsWeights
{
    public double Solved { get; set; } = 1;
    public double SolvedWithRemarks { get; set; } = 0.75;
    public double UnsolvedWithRemarks { get; set; } = 0.25;
    public double Unsolved { get; set; }

    public double Of(GradebookEntryTaskStatus status)
    {
        return status switch
        {
            GradebookEntryTaskStatus.Solved => Solved,
            GradebookEntryTaskStatus.SolvedWithRemarks => SolvedWithRemarks,
            GradebookEntryTaskStatus.UnsolvedWithRemarks => UnsolvedWithRemarks,
            _ => Unsolved,
        };
    }
}
