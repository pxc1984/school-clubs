using FluentValidation;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Application.Common.Exceptions;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;
using ValidationException = SchoolClubs.Application.Common.Exceptions.ValidationException;

namespace SchoolClubs.Application.Services.Admin;

public class AdminStudentService(
    IStudentRepository students,
    IGroupRepository groups,
    IUserRepository users,
    ILessonGradebookRepository gradebooks,
    IUnitOfWork unitOfWork,
    IValidator<CreateStudentRequest> createValidator,
    IValidator<UpdateStudentRequest> updateValidator,
    IValidator<ParseNamesRequest> parseValidator) : IAdminStudentService
{
    public const int SearchLimit = 50;

    public async Task<IReadOnlyList<StudentListItemDto>> SearchAsync(string? query,
        Guid? groupId,
        CancellationToken cancellationToken = default)
    {
        var found = await students.SearchAsync(query, groupId, SearchLimit, cancellationToken);
        var ids = found.Select(s => s.Id).ToList();
        var groupsOf = ids.Count == 0
            ? []
            : await groups.ListWithStudentsByStudentsAsync(ids, cancellationToken);

        return found
            .Select(s => new StudentListItemDto(s.Id, s.LastName, s.FirstName, s.MiddleName, s.Grade,
                groupsOf.Where(g => g.HasStudent(s.Id)).ToRefs()))
            .ToList();
    }

    public async Task<StudentDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await ToDetailsAsync(await FindAsync(id), cancellationToken);
    }

    public async Task<StudentDetailsDto> CreateAsync(CreateStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(createValidator, request, cancellationToken);

        var student = new NewStudentRequest(request.LastName, request.FirstName, request.MiddleName, request.Grade)
            .ToStudent();
        if (request.GroupId is { } groupId)
        {
            var group = await groups.GetWithMembersAsync(groupId, cancellationToken);
            Guard.Against.NotFound(groupId, group);
            group.AddStudent(student);
        }

        await students.AddAsync(student, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailsAsync(student, cancellationToken);
    }

    public async Task<StudentDetailsDto> UpdateAsync(Guid id,
        UpdateStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(updateValidator, request, cancellationToken);

        var student = await FindAsync(id);
        student.Rename(request.FirstName.Trim(), DtoMapping.NullIfBlank(request.MiddleName), request.LastName.Trim());
        student.ChangeGrade(request.Grade);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailsAsync(student, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var student = await FindAsync(id);
        if (await students.CountGradebookEntriesAsync(id, cancellationToken) > 0)
            throw new ConflictException("student has gradebook entries; merge the duplicate instead");

        // Связи с группами и родителями удаляются каскадом в БД.
        students.Remove(student);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MergeStudentsResultDto> MergeAsync(Guid sourceId, Guid targetId,
        CancellationToken cancellationToken = default)
    {
        if (sourceId == targetId)
            throw new ArgumentException("can't merge a student into itself", nameof(targetId));

        var source = await FindAsync(sourceId);
        var target = await FindAsync(targetId);

        var moved = 0;
        var total = 0;
        foreach (var gradebook in await gradebooks.ListByStudentAsync(sourceId, cancellationToken))
        {
            total += gradebook.EntriesOf(sourceId).Count;
            moved += gradebook.MoveEntries(source, target);
        }

        foreach (var group in await groups.ListWithStudentsByStudentsAsync([sourceId], cancellationToken))
        {
            group.AddStudent(target);
            group.RemoveStudent(source);
        }

        foreach (var parent in await users.ListByChildAsync(sourceId, cancellationToken))
        {
            parent.AddChild(target);
            parent.RemoveChild(source);
        }

        // Один SaveChanges — одна транзакция: отметки, связи и удаление дубля применяются вместе.
        students.Remove(source);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new MergeStudentsResultDto(target.Id, moved, total - moved);
    }

    public async Task<IReadOnlyList<ParsedNameDto>> ParseNamesAsync(ParseNamesRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(parseValidator, request, cancellationToken);

        var result = new List<ParsedNameDto>();
        foreach (var line in StudentNameParser.Parse(request.Text))
        {
            var matches = line.Student is null
                ? []
                : (await students.ListByNameAsync(line.Student.LastName, line.Student.FirstName, cancellationToken))
                .ToDtos();
            result.Add(new ParsedNameDto(line.Number, line.Source, line.Student, line.Error, matches));
        }

        return result;
    }

    private async Task<Student> FindAsync(Guid id)
    {
        var student = await students.FindByIdAsync(id);
        Guard.Against.NotFound(id, student);
        return student;
    }

    private async Task<StudentDetailsDto> ToDetailsAsync(Student student, CancellationToken cancellationToken)
    {
        var studentGroups = await groups.ListByStudentAsync(student.Id, cancellationToken);
        var parents = await users.ListByChildAsync(student.Id, cancellationToken);
        var entries = await students.CountGradebookEntriesAsync(student.Id, cancellationToken);
        return new StudentDetailsDto(student.Id, student.LastName, student.FirstName, student.MiddleName,
            student.Grade, studentGroups.ToRefs(), parents.ToRefs(), entries);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);
    }
}
