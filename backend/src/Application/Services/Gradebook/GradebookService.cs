using FluentValidation;
using SchoolClubs.Application.Common;
using SchoolClubs.Application.Services.Lessons;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;
using ValidationException = SchoolClubs.Application.Common.Exceptions.ValidationException;

namespace SchoolClubs.Application.Services.Gradebook;

public class GradebookService(
    TeacherAccess access,
    ILessonGradebookRepository gradebooks,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<SaveGradebookRequest> saveValidator) : IGradebookService
{
    public async Task<GradebookDto> GetAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        var gradebook = await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken);
        return await ToDtoAsync(lesson, group, gradebook);
    }

    public async Task<GradebookDto> SaveAsync(Guid lessonId,
        SaveGradebookRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await saveValidator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);

        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        var gradebook = await GetOrCreateAsync(lesson, cancellationToken);

        foreach (var change in request.Changes)
        {
            var task = lesson.Tasks.FirstOrDefault(t => t.Id == change.TaskId)
                       ?? throw new ArgumentException($"task {change.TaskId} doesn't belong to the lesson");

            if (change.Status is not { } status)
            {
                gradebook.RemoveEntry(change.StudentId, task.Id);
                continue;
            }

            var student = group.Students.FirstOrDefault(s => s.Id == change.StudentId)
                          ?? throw new ArgumentException($"student {change.StudentId} isn't in the lesson's group");
            gradebook.SetEntry(student, task, status, change.Comment);
        }

        foreach (var change in request.Attendance ?? [])
        {
            if (change.Status is not { } attendance)
            {
                gradebook.RemoveAttendance(change.StudentId);
                continue;
            }

            var student = group.Students.FirstOrDefault(s => s.Id == change.StudentId)
                          ?? throw new ArgumentException($"student {change.StudentId} isn't in the lesson's group");
            gradebook.SetAttendance(student, attendance);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(lesson, group, gradebook);
    }

    public async Task<GradebookDto> PublishAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        var gradebook = await GetOrCreateAsync(lesson, cancellationToken);

        gradebook.Publish(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(lesson, group, gradebook);
    }

    public async Task<GradebookDto> UnpublishAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var (lesson, group) = await access.GetLessonAsync(lessonId, cancellationToken);
        var gradebook = await gradebooks.GetByLessonIdAsync(lessonId, cancellationToken);
        if (gradebook is not null)
        {
            gradebook.Unpublish();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await ToDtoAsync(lesson, group, gradebook);
    }

    /// <summary>Журнал создаётся лениво — при первой записи или публикации.</summary>
    private async Task<LessonGradebook> GetOrCreateAsync(Lesson lesson, CancellationToken cancellationToken)
    {
        var gradebook = await gradebooks.GetByLessonIdAsync(lesson.Id, cancellationToken);
        if (gradebook is not null)
            return gradebook;

        gradebook = new LessonGradebook(lesson);
        await gradebooks.AddAsync(gradebook, cancellationToken);
        return gradebook;
    }

    private async Task<GradebookDto> ToDtoAsync(Lesson lesson, Group group, LessonGradebook? gradebook)
    {
        var entries = gradebook?.GradebookEntries.ToList() ?? [];
        var attendance = gradebook?.Attendance.ToList() ?? [];

        var rows = group.Students.Select(s => ToStudent(s, true)).ToList();
        var formerIds = entries.Select(e => e.StudentId)
            .Concat(attendance.Select(a => a.StudentId))
            .Distinct()
            .Where(id => !group.HasStudent(id));
        foreach (var studentId in formerIds)
        {
            if (await students.FindByIdAsync(studentId) is { } former)
                rows.Add(ToStudent(former, false));
        }

        var lessonDto = LessonService.ToDto(lesson, group, gradebook);
        return new GradebookDto(lesson.Id, lesson.Date, lesson.Topic, gradebook?.PublishedAt, lessonDto.Tasks,
            rows.OrderBy(s => !s.InGroup).ThenBy(s => s.LastName).ThenBy(s => s.FirstName).ToList(),
            entries.Select(e => new GradebookCellDto(e.StudentId, e.TaskId, e.Status, e.Comment)).ToList(),
            attendance.Select(a => new GradebookAttendanceDto(a.StudentId, a.Status)).ToList());
    }

    private static GradebookStudentDto ToStudent(Student student, bool inGroup)
    {
        return new GradebookStudentDto(student.Id, student.LastName, student.FirstName, student.MiddleName,
            student.Grade, inGroup);
    }
}
