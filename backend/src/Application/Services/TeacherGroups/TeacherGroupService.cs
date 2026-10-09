using FluentValidation;
using Microsoft.Extensions.Options;
using SchoolClubs.Application.Common;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Application.Common.Options;
using SchoolClubs.Application.Services.Admin;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Application.Services.Lessons;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;
using Task = System.Threading.Tasks.Task;
using ValidationException = SchoolClubs.Application.Common.Exceptions.ValidationException;

namespace SchoolClubs.Application.Services.TeacherGroups;

public class TeacherGroupService(
    TeacherAccess access,
    ICurrentUser currentUser,
    IGroupRepository groups,
    ILessonRepository lessons,
    ILessonGradebookRepository gradebooks,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<SchoolOptions> schoolOptions,
    IValidator<CreateLessonRequest> createLessonValidator,
    IValidator<NewStudentRequest> newStudentValidator) : ITeacherGroupService
{
    public async Task<IReadOnlyList<TeacherGroupCardDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException();

        var today = timeProvider.Today(schoolOptions.Value);
        var cards = new List<TeacherGroupCardDto>();
        foreach (var own in await groups.ListByTeacherAsync(userId, cancellationToken))
        {
            if (own.IsArchived)
                continue;

            var group = await groups.GetWithMembersAsync(own.Id, cancellationToken);
            var overview = await LoadLessonsAsync(group!, cancellationToken);
            cards.Add(new TeacherGroupCardDto(group!.Id, group.Title, group.Description, group.Students.Count,
                overview.Where(l => l.Date <= today).Select(l => (DateOnly?) l.Date).LastOrDefault(),
                overview.Where(l => l.Date > today).Select(l => (DateOnly?) l.Date).FirstOrDefault(),
                overview.Count(l => l.Date <= today && l.FilledPercent < 100)));
        }

        return cards;
    }

    public async Task<TeacherGroupDto> GetAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await access.GetGroupAsync(groupId, cancellationToken);
        return new TeacherGroupDto(group.Id, group.Title, group.Description, group.IsArchived,
            group.Students.ToDtos(), await LoadLessonsAsync(group, cancellationToken));
    }

    public async Task<GroupMatrixDto> GetSummaryAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await access.GetGroupAsync(groupId, cancellationToken);
        var groupLessons = await lessons.ListWithTasksByGroupAsync(groupId, cancellationToken);
        var groupGradebooks = (await gradebooks.ListByGroupAsync(groupId, cancellationToken))
            .ToDictionary(g => g.LessonId);

        var cells = groupGradebooks.Values
            .SelectMany(g => g.GradebookEntries.Select(e => e.StudentId)
                .Concat(g.Attendance.Select(a => a.StudentId))
                .Distinct()
                .Select(studentId =>
                {
                    var entries = g.EntriesOf(studentId);
                    return new SummaryCellDto(studentId, g.LessonId, entries.Count, entries.Count(e => e.IsSolved),
                        g.FindAttendance(studentId)?.Status);
                }))
            .ToList();

        // В матрице и текущий состав, и выбывшие ученики с отметками.
        var summaryStudents = group.Students
            .Select(s => ToSummaryStudent(s, true))
            .ToList();
        foreach (var studentId in cells.Select(c => c.StudentId).Distinct().Where(id => !group.HasStudent(id)))
        {
            if (await students.FindByIdAsync(studentId) is { } former)
                summaryStudents.Add(ToSummaryStudent(former, false));
        }

        var summaryLessons = groupLessons
            .Select(l => new SummaryLessonDto(l.Id, l.Date, l.Topic, l.Tasks.Count,
                groupGradebooks.GetValueOrDefault(l.Id)?.IsPublished == true))
            .ToList();
        return new GroupMatrixDto(summaryLessons,
            summaryStudents.OrderBy(s => !s.InGroup).ThenBy(s => s.LastName).ThenBy(s => s.FirstName).ToList(),
            cells);
    }

    public async Task<IReadOnlyList<LessonSummaryDto>> ListLessonsAsync(Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var group = await access.GetGroupAsync(groupId, cancellationToken);
        return await LoadLessonsAsync(group, cancellationToken);
    }

    public async Task<LessonDto> CreateLessonAsync(Guid groupId,
        CreateLessonRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(createLessonValidator, request, cancellationToken);

        var group = await access.GetGroupAsync(groupId, cancellationToken);
        var taskCount = request.TaskCount;
        if (taskCount is null)
        {
            var previous = (await lessons.ListWithTasksByGroupAsync(groupId, cancellationToken)).LastOrDefault();
            taskCount = previous is { Tasks.Count: > 0 } ? previous.Tasks.Count : 1;
        }

        var lesson = new Lesson(group, request.Date, request.Time, request.Topic);
        for (var i = 0; i < taskCount; i++)
            lesson.AddTask();

        await lessons.AddAsync(lesson, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return LessonService.ToDto(lesson, group, null);
    }

    public async Task<StudentDto> AddStudentAsync(Guid groupId,
        NewStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(newStudentValidator, request, cancellationToken);

        var group = await access.GetGroupAsync(groupId, cancellationToken);
        var student = request.ToStudent();
        await students.AddAsync(student, cancellationToken);
        group.AddStudent(student);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return student.ToDto();
    }

    public async Task RemoveStudentAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken = default)
    {
        var group = await access.GetGroupAsync(groupId, cancellationToken);
        var student = group.Students.FirstOrDefault(s => s.Id == studentId);
        Guard.Against.NotFound(studentId, student);

        group.RemoveStudent(student);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<LessonSummaryDto>> LoadLessonsAsync(Group group,
        CancellationToken cancellationToken)
    {
        var groupLessons = await lessons.ListWithTasksByGroupAsync(group.Id, cancellationToken);
        var groupGradebooks = (await gradebooks.ListByGroupAsync(group.Id, cancellationToken))
            .ToDictionary(g => g.LessonId);

        return groupLessons
            .Select(l =>
            {
                var gradebook = groupGradebooks.GetValueOrDefault(l.Id);
                var cells = l.Tasks.Count * group.Students.Count;
                // У отмеченных «не был» / «опоздал» пустые ячейки ожидаемы — строка считается заполненной.
                var filled = gradebook is null
                    ? 0
                    : group.Students.Sum(s => gradebook.FindAttendance(s.Id) != null
                        ? l.Tasks.Count
                        : gradebook.EntriesOf(s.Id).Count);
                var percent = cells == 0 ? 0 : Math.Min(100, filled * 100 / cells);
                return new LessonSummaryDto(l.Id, l.Date, l.Time, l.Topic, l.Tasks.Count, percent,
                    gradebook?.IsPublished == true);
            })
            .ToList();
    }

    private static SummaryStudentDto ToSummaryStudent(Student student, bool inGroup)
    {
        return new SummaryStudentDto(student.Id, student.LastName, student.FirstName, student.MiddleName,
            student.Grade, inGroup);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);
    }
}
