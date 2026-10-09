using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Application.Services.GradebookImport;

/// <summary>Символы отметок в трафарете и синонимы, которыми их вводят на самом деле.</summary>
public static class GradebookSymbols
{
    public const string Solved = "+";
    public const string SolvedWithRemarks = "+.";
    public const string UnsolvedWithRemarks = "-.";
    public const string Unsolved = "-";

    public static readonly IReadOnlyList<string> All = [Solved, SolvedWithRemarks, UnsolvedWithRemarks, Unsolved];

    private static readonly Dictionary<string, GradebookEntryTaskStatus> Synonyms = new()
    {
        ["+"] = GradebookEntryTaskStatus.Solved,
        ["1"] = GradebookEntryTaskStatus.Solved,
        ["да"] = GradebookEntryTaskStatus.Solved,
        ["✓"] = GradebookEntryTaskStatus.Solved,
        ["✔"] = GradebookEntryTaskStatus.Solved,
        ["v"] = GradebookEntryTaskStatus.Solved,
        ["+."] = GradebookEntryTaskStatus.SolvedWithRemarks,
        ["+/-"] = GradebookEntryTaskStatus.SolvedWithRemarks,
        ["±"] = GradebookEntryTaskStatus.SolvedWithRemarks,
        ["-."] = GradebookEntryTaskStatus.UnsolvedWithRemarks,
        ["-/+"] = GradebookEntryTaskStatus.UnsolvedWithRemarks,
        ["-"] = GradebookEntryTaskStatus.Unsolved,
        ["0"] = GradebookEntryTaskStatus.Unsolved,
        ["нет"] = GradebookEntryTaskStatus.Unsolved,
    };

    public static string ToSymbol(GradebookEntryTaskStatus status)
    {
        return status switch
        {
            GradebookEntryTaskStatus.Solved => Solved,
            GradebookEntryTaskStatus.SolvedWithRemarks => SolvedWithRemarks,
            GradebookEntryTaskStatus.UnsolvedWithRemarks => UnsolvedWithRemarks,
            GradebookEntryTaskStatus.Unsolved => Unsolved,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "unknown task status"),
        };
    }

    /// <summary>Регистр и пробелы игнорируются; дефисы и минусы разных видов считаются одним символом.</summary>
    public static bool TryParse(string text, out GradebookEntryTaskStatus status)
    {
        var normalized = string.Concat(text.Where(c => !char.IsWhiteSpace(c)))
            .ToLowerInvariant()
            .Replace('−', '-')
            .Replace('–', '-')
            .Replace('—', '-')
            .Replace(',', '.');
        return Synonyms.TryGetValue(normalized, out status);
    }
}
