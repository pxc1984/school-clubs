using LessonGradebook = SchoolClubs.Domain.Entities.Gradebook.LessonGradebook;

namespace SchoolClubs.Domain.Abstractions;

public interface ILessonGradebookRepository : IRepository<LessonGradebook>
{
    /// <summary>Журнал занятия вместе с занятием, его задачами, записями журнала и посещаемостью.</summary>
    Task<LessonGradebook?> GetByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default);

    /// <summary>Журналы всех занятий группы с записями и посещаемостью (без занятий и задач).</summary>
    Task<IReadOnlyList<LessonGradebook>> ListByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>Журналы, где у ученика есть отметки или посещаемость, — с занятием, задачами и всеми записями.</summary>
    Task<IReadOnlyList<LessonGradebook>> ListByStudentAsync(Guid studentId,
        CancellationToken cancellationToken = default);
}
