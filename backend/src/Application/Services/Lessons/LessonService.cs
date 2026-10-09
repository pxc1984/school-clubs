using FluentValidation;
using FluentValidation.Results;
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
    IValidator<TaskRequest> taskValidator) : ILessonService
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
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(taskValidator, request, cancellationToken);

        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        if (lesson.Tasks.Count >= MaxTasks)
            throw new ArgumentException($"lesson can't have more than {MaxTasks} tasks");

        EnsureNumberIsFree(lesson, request.Number, null);
        lesson.AddTask(request.Title, request.Number);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(lesson, group, await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken));
    }

    public async Task<LessonDto> UpdateTaskAsync(Guid lessonId,
        Guid taskId,
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(taskValidator, request, cancellationToken);

        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        var task = FindTask(lesson, taskId);
        if (request.Number is not null)
        {
            EnsureNumberIsFree(lesson, request.Number, task);
            lesson.RenumberTask(task, request.Number);
        }

        task.Rename(request.Title);
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
            .Select(t => new TaskDto(t.Id, t.Number, t.Title,
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

    private static void EnsureNumberIsFree(Lesson lesson, string? number, Domain.Entities.Gradebook.Task? except)
    {
        if (number is not null && lesson.FindTaskByNumber(number) is { } other && other != except)
            throw new ValidationException([
                new ValidationFailure(nameof(TaskRequest.Number), $"Задача с номером {other.Number} уже есть."),
            ]);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);
    }
}
