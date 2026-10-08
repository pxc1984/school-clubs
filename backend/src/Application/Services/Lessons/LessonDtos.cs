namespace SchoolClubs.Application.Services.Lessons;

/// <param name="EntryCount">Сколько отметок удалится вместе с задачей — UI переспрашивает с этим числом.</param>
public record TaskDto(Guid Id, long Order, string? Title, int EntryCount);

public record LessonDto(
    Guid Id,
    Guid GroupId,
    string GroupTitle,
    DateOnly Date,
    TimeOnly? Time,
    string? Topic,
    IReadOnlyList<TaskDto> Tasks,
    DateTimeOffset? PublishedAt);

/// <param name="TaskCount">По умолчанию — как в последнем занятии группы (или 1).</param>
public record CreateLessonRequest(DateOnly Date, TimeOnly? Time, string? Topic, int? TaskCount);

public record UpdateLessonRequest(DateOnly Date, TimeOnly? Time, string? Topic);

public record TaskTitleRequest(string? Title);
