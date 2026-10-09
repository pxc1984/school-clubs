using FluentValidation;
using FluentValidation.Results;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Access;
using ValidationException = SchoolClubs.Application.Common.Exceptions.ValidationException;

namespace SchoolClubs.Application.Services.AccessRequests;

public class AccessRequestService(
    IAccessRequestRepository requests,
    IGroupRepository groups,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IValidator<CreateAccessRequestRequest> createValidator) : IAccessRequestService
{
    public const int MaxPendingPerParent = 5;

    public async Task<IReadOnlyList<ParentGroupDto>> ListGroupsAsync(CancellationToken cancellationToken = default)
    {
        var active = await groups.ListWithMembersAsync(false, cancellationToken);
        return active.Select(g => new ParentGroupDto(g.Id, g.Title, g.Description)).ToList();
    }

    public async Task<IReadOnlyList<AccessRequestDto>> ListMineAsync(CancellationToken cancellationToken = default)
    {
        var mine = await requests.ListByParentAsync(CurrentUserId(), cancellationToken);
        var groupRefs = await GroupRefsAsync(mine.Select(r => r.GroupId), cancellationToken);
        return mine.Select(r => ToDto(r, groupRefs[r.GroupId])).ToList();
    }

    public async Task<AccessRequestDto> CreateAsync(CreateAccessRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await createValidator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);

        var userId = CurrentUserId();
        var parent = await users.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException();
        var relation = request.Relation ?? ParentRelations.FromGender(parent.Gender)
            ?? throw new ValidationException([
                new ValidationFailure(nameof(CreateAccessRequestRequest.Relation), "Укажите, кем приходитесь ребёнку."),
            ]);
        var group = await groups.FindByIdAsync(request.GroupId);
        if (group is null || group.IsArchived)
            throw new ArgumentException("group isn't available for requests", nameof(request.GroupId));

        if (await requests.CountPendingAsync(userId, cancellationToken) >= MaxPendingPerParent)
            throw new ArgumentException($"no more than {MaxPendingPerParent} pending requests are allowed");
        if (await requests.HasPendingDuplicateAsync(userId, group.Id, request.ChildLastName, request.ChildFirstName,
                cancellationToken))
            throw new ArgumentException("the same request is already pending");

        var accessRequest = new AccessRequest(parent, group.Id, request.ChildLastName, request.ChildFirstName,
            request.ChildMiddleName, request.Grade, relation, request.Comment, timeProvider.GetUtcNow());
        await requests.AddAsync(accessRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(accessRequest, group.ToRef());
    }

    public async Task<AccessRequestDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var accessRequest = await requests.FindByIdAsync(id);
        // Чужой запрос — как несуществующий.
        if (accessRequest is null || accessRequest.UserId != CurrentUserId())
            throw new NotFoundException(id.ToString(), nameof(AccessRequest));

        accessRequest.Cancel(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var groupRefs = await GroupRefsAsync([accessRequest.GroupId], cancellationToken);
        return ToDto(accessRequest, groupRefs[accessRequest.GroupId]);
    }

    private Guid CurrentUserId()
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }

    private async Task<Dictionary<Guid, GroupRefDto>> GroupRefsAsync(IEnumerable<Guid> ids,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, GroupRefDto>();
        foreach (var id in ids.Distinct())
        {
            var group = await groups.FindByIdAsync(id);
            result[id] = group?.ToRef() ?? new GroupRefDto(id, "", true);
        }

        return result;
    }

    internal static AccessRequestDto ToDto(AccessRequest r, GroupRefDto group)
    {
        return new AccessRequestDto(r.Id, group, r.ChildLastName, r.ChildFirstName, r.ChildMiddleName, r.Grade,
            r.Relation, r.Comment, r.Status, r.RejectReason, r.Created, r.DecidedAt);
    }
}
