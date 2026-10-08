using Lesson = SchoolClubs.Domain.Entities.Gradebook.Lesson;

namespace SchoolClubs.Domain.Abstractions;

public interface ILessonRepository : IRepository<Lesson>
{
    /// <summary>Занятие вместе с задачами.</summary>
    Task<Lesson?> GetWithTasksAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Все занятия группы с задачами, по возрастанию даты и времени.</summary>
    Task<IReadOnlyList<Lesson>> ListWithTasksByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>Занятия группы в диапазоне дат (границы включительно), по возрастанию даты и времени.</summary>
    Task<IReadOnlyList<Lesson>> ListByGroupAsync(
        Guid groupId,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);
}
