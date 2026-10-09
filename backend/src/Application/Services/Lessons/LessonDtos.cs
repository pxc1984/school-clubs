namespace SchoolClubs.Application.Services.Lessons;

/// <param name="EntryCount">Сколько отметок удалится вместе с задачей — UI переспрашивает с этим числом.</param>
/// <param name="Number">Номер в свободной форме («1.1», «2а»); задачи приходят в порядке номеров.</param>
public record TaskDto(Guid Id, string Number, string? Title, int EntryCount);

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

/// <param name="Number">Новый номер; <c>null</c> — при добавлении следующий по порядку, при изменении — прежний.</param>
public record TaskRequest(string? Number, string? Title);
