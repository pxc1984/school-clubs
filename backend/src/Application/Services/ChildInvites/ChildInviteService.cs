using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Access;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Services.ChildInvites;

public class ChildInviteService(
    IChildInviteRepository invites,
    IUserRepository users,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IChildInviteService
{
    public async Task<AcceptedChildInviteDto> AcceptAsync(string token, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var invite = token.Length == ChildInvite.TokenLength
            ? await invites.FindByTokenAsync(token, cancellationToken)
            : null;
        if (invite is null)
            throw new NotFoundException(token, nameof(ChildInvite));
        var student = await students.FindByIdAsync(invite.StudentId);
        Guard.Against.NotFound(invite.StudentId, student);

        // Повторный переход по своей же ссылке (обновил страницу, открыл письмо ещё раз).
        if (invite.IsAcceptedBy(userId))
            return new AcceptedChildInviteDto(student.ToDto(), true);

        var parent = await users.GetWithChildrenAsync(userId, cancellationToken)
                     ?? throw new UnauthorizedAccessException();
        invite.Accept(parent, timeProvider.GetUtcNow());
        parent.GrantRole(UserRole.Parent);
        var added = parent.AddChild(student);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new AcceptedChildInviteDto(student.ToDto(), !added);
    }
}
