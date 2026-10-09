namespace SchoolClubs.Application.Services.ParentDashboard;

/// <summary>Кабинет родителя: только свои дети и только опубликованные журналы.</summary>
public interface IParentDashboardService
{
    Task<IReadOnlyList<ChildDto>> ListChildrenAsync(CancellationToken cancellationToken = default);

    Task<ChildSummaryDto> GetSummaryAsync(Guid studentId,
        Guid groupId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChildLessonDto>> ListLessonsAsync(Guid studentId,
        Guid groupId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);
}
