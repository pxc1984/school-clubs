using System.Security.Cryptography;
using FluentValidation;
using SchoolClubs.Application.Common.Exceptions;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Common.Exceptions;
using SchoolClubs.Domain.Entities.Users;
using ValidationException = SchoolClubs.Application.Common.Exceptions.ValidationException;

namespace SchoolClubs.Application.Services.Admin;

public class AdminUserService(
    IUserRepository users,
    IUserLoginRepository logins,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IValidator<SetRolesRequest> rolesValidator,
    IValidator<InviteTeacherRequest> inviteValidator) : IAdminUserService
{
    public const int SearchLimit = 50;

    // Без похожих символов (0/O, 1/l/I), чтобы временный пароль можно было продиктовать.
    private const string PasswordAlphabet = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int TemporaryPasswordLength = 12;

    public async Task<IReadOnlyList<UserSummaryDto>> SearchAsync(string? query,
        UserRole? role,
        CancellationToken cancellationToken = default)
    {
        var found = await users.SearchAsync(query, role, SearchLimit, cancellationToken);
        return found
            .Select(u => new UserSummaryDto(u.Id, u.LastName, u.FirstName, u.MiddleName, u.Email.Address, u.Phone,
                u.Roles.Order().ToList(), u.IsBlocked))
            .ToList();
    }

    public async Task<UserDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await ToDetailsAsync(await FindAsync(id, cancellationToken), cancellationToken);
    }

    public async Task<UserDetailsDto> SetRolesAsync(Guid id,
        SetRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await rolesValidator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);

        var user = await FindAsync(id, cancellationToken);
        if (user.Id == currentUser.UserId && user.HasRole(UserRole.Admin) && !request.Roles.Contains(UserRole.Admin))
            throw new UserRoleException("you can't revoke your own admin role");

        foreach (var role in request.Roles)
            user.GrantRole(role);
        foreach (var role in user.Roles.Except(request.Roles).ToList())
            user.RevokeRole(role);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailsAsync(user, cancellationToken);
    }

    public async Task<UserDetailsDto> BlockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(id, cancellationToken);
        if (user.Id == currentUser.UserId)
            throw new ArgumentException("you can't block yourself", nameof(id));

        user.Block();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailsAsync(user, cancellationToken);
    }

    public async Task<UserDetailsDto> UnblockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(id, cancellationToken);
        user.Unblock();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailsAsync(user, cancellationToken);
    }

    public async Task<UserDetailsDto> InviteTeacherAsync(InviteTeacherRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await inviteValidator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);

        var existing = await users.FindByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            existing.GrantRole(UserRole.Teacher);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return await GetAsync(existing.Id, cancellationToken);
        }

        var teacher = new User(request.FirstName.Trim(), AdminMapping.NullIfBlank(request.MiddleName),
            request.LastName.Trim(), request.Email, roles: [UserRole.Teacher]);
        await users.AddAsync(teacher, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailsAsync(teacher, cancellationToken);
    }

    public async Task<TemporaryPasswordDto> ResetPasswordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(id, cancellationToken);
        var password = RandomNumberGenerator.GetString(PasswordAlphabet, TemporaryPasswordLength);
        var hash = passwordHasher.Hash(password);

        var userLogins = await logins.ListByUserAsync(user.Id, cancellationToken);
        var passwordLogin = userLogins.FirstOrDefault(l => !l.IsExternal);
        if (passwordLogin is not null)
        {
            passwordLogin.ChangePasswordHash(hash);
        }
        else
        {
            // Вход по паролю привязан к текущему email пользователя.
            if (await logins.FindAsync(UserLogin.PasswordProvider, user.Email.Address, cancellationToken) is not null)
                throw new ConflictException("another account already uses this email for password login");
            await logins.AddAsync(UserLogin.ForPassword(user, user.Email.Address, hash), cancellationToken);
        }

        user.RotateSecurityStamp();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new TemporaryPasswordDto(password);
    }

    public async Task<UserDetailsDto> LinkChildAsync(Guid parentId, Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var parent = await FindAsync(parentId, cancellationToken);
        var student = await students.FindByIdAsync(studentId);
        Guard.Against.NotFound(studentId, student);

        parent.GrantRole(UserRole.Parent);
        parent.AddChild(student);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailsAsync(parent, cancellationToken);
    }

    public async Task<UserDetailsDto> UnlinkChildAsync(Guid parentId, Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var parent = await FindAsync(parentId, cancellationToken);
        var child = parent.Children.FirstOrDefault(c => c.Id == studentId);
        Guard.Against.NotFound(studentId, child);

        parent.RemoveChild(child);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailsAsync(parent, cancellationToken);
    }

    /// <summary>С детьми и группами: без них доменные проверки RevokeRole не сработают.</summary>
    private async Task<User> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await users.GetWithLinksAsync(id, cancellationToken);
        Guard.Against.NotFound(id, user);
        return user;
    }

    private async Task<UserDetailsDto> ToDetailsAsync(User user, CancellationToken cancellationToken)
    {
        var providers = (await logins.ListByUserAsync(user.Id, cancellationToken)).Select(l => l.Provider).ToList();
        return new UserDetailsDto(user.Id, user.LastName, user.FirstName, user.MiddleName, user.Email.Address,
            user.Phone, user.Roles.Order().ToList(), user.IsBlocked, user.ConsentAcceptedAt, providers,
            user.Children.ToDtos(), user.LinkedGroups.ToRefs());
    }
}
