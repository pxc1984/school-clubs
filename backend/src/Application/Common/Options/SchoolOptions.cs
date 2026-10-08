namespace SchoolClubs.Application.Common.Options;

/// <summary>Секция <c>School</c>.</summary>
public class SchoolOptions
{
    public const string SectionName = "School";

    /// <summary>Часовой пояс школы: от него считается «сегодня» (незаполненные журналы и т.п.).</summary>
    public string TimeZone { get; set; } = "Europe/Moscow";
}

public static class SchoolClock
{
    /// <summary>Текущая дата в часовом поясе школы.</summary>
    public static DateOnly Today(this TimeProvider timeProvider, SchoolOptions options)
    {
        var zone = TimeZoneInfo.TryFindSystemTimeZoneById(options.TimeZone, out var found)
            ? found
            : TimeZoneInfo.CreateCustomTimeZone("MSK", TimeSpan.FromHours(3), "MSK", "MSK");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), zone).DateTime);
    }
}
