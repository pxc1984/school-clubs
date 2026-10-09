using FluentValidation;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Access;
using SchoolClubs.Domain.Entities.Users;
using Group = SchoolClubs.Domain.Entities.Gradebook.Group;
using ValidationException = SchoolClubs.Application.Common.Exceptions.ValidationException;

namespace SchoolClubs.Application.Services.AccessRequests;

public class AdminAccessRequestService(
    IAccessRequestRepository requests,
    IGroupRepository groups,
    IUserRepository users,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<ApproveAccessRequestRequest> approveValidator,
    IValidator<RejectAccessRequestRequest> rejectValidator) : IAdminAccessRequestService
{
    public const int ListLimit = 200;

    public async Task<IReadOnlyList<AdminAccessRequestDto>> ListAsync(AccessRequestStatus? status,
        CancellationToken cancellationToken = default)
    {
        var list = await requests.ListByStatusAsync(status, ListLimit, cancellationToken);
        var cards = new List<AdminAccessRequestDto>();
        foreach (var accessRequest in list)
            cards.Add(await ToCardAsync(accessRequest, cancellationToken));
        return cards;
    }

    public async Task<AdminAccessRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await ToCardAsync(await FindAsync(id), cancellationToken);
    }

    public async Task<PendingCountDto> CountPendingAsync(CancellationToken cancellationToken = default)
    {
        return new PendingCountDto(await requests.CountPendingAsync(null, cancellationToken));
    }

    public async Task<AdminAccessRequestDto> ApproveAsync(Guid id,
        ApproveAccessRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await approveValidator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);

        var accessRequest = await FindAsync(id);
        var parent = await users.GetWithChildrenAsync(accessRequest.UserId, cancellationToken);
        Guard.Against.NotFound(accessRequest.UserId, parent);
        var group = await groups.GetWithMembersAsync(accessRequest.GroupId, cancellationToken);
        Guard.Against.NotFound(accessRequest.GroupId, group);

        Student student;
        if (request.StudentId is { } studentId)
        {
            var existing = await students.FindByIdAsync(studentId);
            Guard.Against.NotFound(studentId, existing);
            student = existing;
        }
        else
        {
            student = request.NewStudent!.ToStudent();
            await students.AddAsync(student, cancellationToken);
        }

        // Всё — в одном SaveChanges, то есть в одной транзакции.
        accessRequest.Approve(student, timeProvider.GetUtcNow());
        parent.GrantRole(UserRole.Parent);
        parent.AddChild(student);
        group.AddStudent(student);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToCardAsync(accessRequest, cancellationToken);
    }

    public async Task<AdminAccessRequestDto> RejectAsync(Guid id,
        RejectAccessRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await rejectValidator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);

        var accessRequest = await FindAsync(id);
        accessRequest.Reject(request.Reason, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToCardAsync(accessRequest, cancellationToken);
    }

    private async Task<AccessRequest> FindAsync(Guid id)
    {
        var accessRequest = await requests.FindByIdAsync(id);
        Guard.Against.NotFound(id, accessRequest);
        return accessRequest;
    }

    private async Task<AdminAccessRequestDto> ToCardAsync(AccessRequest r, CancellationToken cancellationToken)
    {
        var parent = await users.GetWithChildrenAsync(r.UserId, cancellationToken);
        var group = await groups.GetWithMembersAsync(r.GroupId, cancellationToken);
        var student = r.StudentId is { } studentId ? await students.FindByIdAsync(studentId) : null;

        var parentDto = parent is null
            ? new RequestParentDto(r.UserId, "", "", "", "", null, [])
            : new RequestParentDto(parent.Id, parent.LastName, parent.FirstName, parent.MiddleName,
                parent.Email.Address, parent.Phone, parent.Children.ToDtos());
        var groupRef = group?.ToRef() ?? new GroupRefDto(r.GroupId, "", true);
        var suggestions = r.IsPending && group is not null ? Suggest(r, group) : [];

        return new AdminAccessRequestDto(r.Id, parentDto, groupRef, r.ChildLastName, r.ChildFirstName,
            r.ChildMiddleName, r.Grade, r.Relation, r.Comment, r.Status, r.RejectReason, r.Created, r.DecidedAt,
            student?.ToDto(), suggestions);
    }

    /// <summary>Похожие ученики группы: сначала совпадение фамилии и имени, затем только фамилии или имени.</summary>
    private static IReadOnlyList<StudentDto> Suggest(AccessRequest r, Group group)
    {
        var last = Names.Normalize(r.ChildLastName);
        var first = Names.Normalize(r.ChildFirstName);
        return group.Students
            .Select(s => (Student: s,
                Score: (Names.Normalize(s.LastName) == last ? 2 : 0)
                       + (Names.Normalize(s.FirstName) == first ? 1 : 0)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Student.LastName)
            .Select(x => x.Student.ToDto())
            .ToList();
    }
}
