using FluentValidation;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Application.Common.Exceptions;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;
using Task = System.Threading.Tasks.Task;
using ValidationException = SchoolClubs.Application.Common.Exceptions.ValidationException;

namespace SchoolClubs.Application.Services.Admin;

public class AdminGroupService(
    IGroupRepository groups,
    IUserRepository users,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    IValidator<CreateGroupRequest> createValidator,
    IValidator<UpdateGroupRequest> updateValidator,
    IValidator<AddGroupStudentsRequest> addStudentsValidator) : IAdminGroupService
{
    public async Task<IReadOnlyList<GroupSummaryDto>> ListAsync(bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var list = await groups.ListWithMembersAsync(includeArchived, cancellationToken);
        return list
            .Select(g => new GroupSummaryDto(g.Id, g.Title, g.Description, g.IsArchived, g.Teachers.ToRefs(),
                g.Students.Count))
            .ToList();
    }

    public async Task<GroupDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ToDetails(await FindAsync(id, cancellationToken));
    }

    public async Task<GroupDetailsDto> CreateAsync(CreateGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(createValidator, request, cancellationToken);

        var group = new Group(request.Title, request.Description);
        await groups.AddAsync(group, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDetails(group);
    }

    public async Task<GroupDetailsDto> UpdateAsync(Guid id,
        UpdateGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(updateValidator, request, cancellationToken);

        var group = await FindAsync(id, cancellationToken);
        group.Rename(request.Title, request.Description);
        if (request.IsArchived)
            group.Archive();
        else
            group.Restore();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDetails(group);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await FindAsync(id, cancellationToken);
        if (await groups.HasLessonsAsync(id, cancellationToken))
            throw new ConflictException("group has lessons; archive it instead");

        groups.Remove(group);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<GroupDetailsDto> AddTeacherAsync(Guid groupId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var group = await FindAsync(groupId, cancellationToken);
        var teacher = await users.GetWithGroupsAsync(userId, cancellationToken);
        Guard.Against.NotFound(userId, teacher);

        group.AddTeacher(teacher);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDetails(group);
    }

    public async Task<GroupDetailsDto> RemoveTeacherAsync(Guid groupId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var group = await FindAsync(groupId, cancellationToken);
        var teacher = await users.GetWithGroupsAsync(userId, cancellationToken);
        Guard.Against.NotFound(userId, teacher);

        group.RemoveTeacher(teacher);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDetails(group);
    }

    public async Task<GroupDetailsDto> AddStudentsAsync(Guid groupId,
        AddGroupStudentsRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(addStudentsValidator, request, cancellationToken);

        var group = await FindAsync(groupId, cancellationToken);
        foreach (var studentId in request.StudentIds ?? [])
        {
            var existing = await students.FindByIdAsync(studentId);
            Guard.Against.NotFound(studentId, existing);
            group.AddStudent(existing);
        }

        foreach (var newStudent in request.NewStudents ?? [])
        {
            var student = newStudent.ToStudent();
            await students.AddAsync(student, cancellationToken);
            group.AddStudent(student);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDetails(group);
    }

    public async Task<GroupDetailsDto> RemoveStudentAsync(Guid groupId, Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var group = await FindAsync(groupId, cancellationToken);
        var student = group.Students.FirstOrDefault(s => s.Id == studentId);
        Guard.Against.NotFound(studentId, student);

        // Отметки выбывшего ученика остаются в журналах.
        group.RemoveStudent(student);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDetails(group);
    }

    private async Task<Group> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var group = await groups.GetWithMembersAsync(id, cancellationToken);
        Guard.Against.NotFound(id, group);
        return group;
    }

    private static GroupDetailsDto ToDetails(Group group)
    {
        return new GroupDetailsDto(group.Id, group.Title, group.Description, group.IsArchived,
            group.Teachers.ToRefs(), group.Students.ToDtos());
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);
    }
}
