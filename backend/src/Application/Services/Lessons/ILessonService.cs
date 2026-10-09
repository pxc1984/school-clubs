namespace SchoolClubs.Application.Services.Lessons;

public interface ILessonService
{
    Task<LessonDto> GetAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task<LessonDto> UpdateAsync(Guid lessonId, UpdateLessonRequest request, CancellationToken cancellationToken = default);

    /// <summary>Только занятие без отметок; иначе — через админа.</summary>
    Task DeleteAsync(Guid lessonId, CancellationToken cancellationToken = default);

    Task<LessonDto> AddTaskAsync(Guid lessonId, TaskRequest request, CancellationToken cancellationToken = default);

    Task<LessonDto> UpdateTaskAsync(Guid lessonId,
        Guid taskId,
        TaskRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Удаляет задачу вместе с её отметками и сдвигает номера следующих.</summary>
    Task<LessonDto> DeleteTaskAsync(Guid lessonId, Guid taskId, CancellationToken cancellationToken = default);
}
