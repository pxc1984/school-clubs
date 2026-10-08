using FluentValidation;
using SchoolClubs.Application.Common;
using SchoolClubs.Application.Common.Exceptions;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;
using Task = System.Threading.Tasks.Task;
using ValidationException = SchoolClubs.Application.Common.Exceptions.ValidationException;

namespace SchoolClubs.Application.Services.Lessons;

public class LessonService(
    TeacherAccess access,
    ILessonRepository lessons,
    ILessonGradebookRepository gradebooks,
    IUnitOfWork unitOfWork,
    IValidator<UpdateLessonRequest> updateValidator,
    IValidator<TaskTitleRequest> taskTitleValidator) : ILessonService
{
    /// <summary>Как у XLSX-трафарета: больше задач в одном занятии не бывает.</summary>
    public const int MaxTasks = 50;

    public async Task<LessonDto> GetAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        var gradebook = await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken);
        return ToDto(lesson, group, gradebook);
    }

    public async Task<LessonDto> UpdateAsync(Guid lessonId,
        UpdateLessonRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(updateValidator, request, cancellationToken);

        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        lesson.Reschedule(request.Date, request.Time);
        lesson.ChangeTopic(request.Topic);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(lesson, group, await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken));
    }

    public async Task DeleteAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var (lesson, _) = await access.GetLessonAsync(lessonId, cancellationToken);
        var gradebook = await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken);
        if (gradebook?.GradebookEntries.Count > 0)
            throw new ConflictException("lesson has gradebook entries; ask an admin to delete it");

        lessons.Remove(lesson);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<LessonDto> AddTaskAsync(Guid lessonId,
        TaskTitleRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(taskTitleValidator, request, cancellationToken);

        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        if (lesson.Tasks.Count >= MaxTasks)
            throw new ArgumentException($"lesson can't have more than {MaxTasks} tasks");

        lesson.AddTask(request.Title);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(lesson, group, await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken));
    }

    public async Task<LessonDto> RenameTaskAsync(Guid lessonId,
        Guid taskId,
        TaskTitleRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(taskTitleValidator, request, cancellationToken);

        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        FindTask(lesson, taskId).Rename(request.Title);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(lesson, group, await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken));
    }

    public async Task<LessonDto> DeleteTaskAsync(Guid lessonId, Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        var task = FindTask(lesson, taskId);

        var gradebook = await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken);
        foreach (var entry in gradebook?.GradebookEntries.Where(e => e.TaskId == taskId).ToList() ?? [])
            gradebook!.RemoveEntry(entry.StudentId, entry.TaskId);
        lesson.RemoveTask(task);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(lesson, group, gradebook);
    }

    internal static LessonDto ToDto(Lesson lesson, Group group, LessonGradebook? gradebook)
    {
        var tasks = lesson.Tasks
            .OrderBy(t => t.Order)
            .Select(t => new TaskDto(t.Id, t.Order, t.Title,
                gradebook?.GradebookEntries.Count(e => e.TaskId == t.Id) ?? 0))
            .ToList();
        return new LessonDto(lesson.Id, group.Id, group.Title, lesson.Date, lesson.Time, lesson.Topic, tasks,
            gradebook?.PublishedAt);
    }

    private static Domain.Entities.Gradebook.Task FindTask(Lesson lesson, Guid taskId)
    {
        var task = lesson.Tasks.FirstOrDefault(t => t.Id == taskId);
        Guard.Against.NotFound(taskId, task);
        return task;
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);
    }
}
