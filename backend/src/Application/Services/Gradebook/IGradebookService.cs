namespace SchoolClubs.Application.Services.Gradebook;

public interface IGradebookService
{
    Task<GradebookDto> GetAsync(Guid lessonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Применяет изменения ячеек и посещаемости одной транзакцией; журнал создаётся при первой записи.
    /// Ставить отметку (и «не был» / «опоздал») можно только ученику из группы занятия.
    /// </summary>
    Task<GradebookDto> SaveAsync(Guid lessonId, SaveGradebookRequest request, CancellationToken cancellationToken = default);

    /// <summary>Открывает журнал родителям.</summary>
    Task<GradebookDto> PublishAsync(Guid lessonId, CancellationToken cancellationToken = default);

    Task<GradebookDto> UnpublishAsync(Guid lessonId, CancellationToken cancellationToken = default);
}
