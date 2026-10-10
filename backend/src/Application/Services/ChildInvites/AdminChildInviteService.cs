using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Access;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Services.ChildInvites;

public class AdminChildInviteService(
    IChildInviteRepository invites,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IAdminChildInviteService
{
    public async Task<IReadOnlyList<ChildInviteDto>> ListAsync(Guid studentId,
        CancellationToken cancellationToken = default)
    {
        await FindStudentAsync(studentId);
        var active = await invites.ListActiveByStudentAsync(studentId, timeProvider.GetUtcNow(), cancellationToken);
        return active.Select(ToDto).ToList();
    }

    public async Task<ChildInviteDto> CreateAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await FindStudentAsync(studentId);
        var invite = new ChildInvite(student, timeProvider.GetUtcNow());
        await invites.AddAsync(invite, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(invite);
    }

    public async Task RevokeAsync(Guid studentId, Guid inviteId, CancellationToken cancellationToken = default)
    {
        var invite = await invites.FindByIdAsync(inviteId);
        if (invite is null || invite.StudentId != studentId)
            throw new NotFoundException(inviteId.ToString(), nameof(ChildInvite));

        invite.Revoke(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Student> FindStudentAsync(Guid id)
    {
        var student = await students.FindByIdAsync(id);
        Guard.Against.NotFound(id, student);
        return student;
    }

    private static ChildInviteDto ToDto(ChildInvite invite)
    {
        return new ChildInviteDto(invite.Id, invite.Token, invite.Created, invite.ExpiresAt);
    }
}
