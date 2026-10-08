using LessonGradebook = SchoolClubs.Domain.Entities.Gradebook.LessonGradebook;

namespace SchoolClubs.Domain.Abstractions;

public interface ILessonGradebookRepository : IRepository<LessonGradebook>
{
    /// <summary>Журнал занятия вместе с занятием, его задачами и записями журнала.</summary>
    Task<LessonGradebook?> GetByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default);

    /// <summary>Журналы, где у ученика есть отметки, — с занятием, задачами и всеми записями.</summary>
    Task<IReadOnlyList<LessonGradebook>> ListByStudentAsync(Guid studentId,
        CancellationToken cancellationToken = default);
}
