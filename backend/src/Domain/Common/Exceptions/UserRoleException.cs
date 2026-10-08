using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Common.Exceptions;

/// <summary>Действие нарушает правила ролей пользователя.</summary>
public class UserRoleException(string message) : ArgumentException(message)
{
    public static void ThrowIfMissing(User user, UserRole role)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!user.HasRole(role))
            throw new UserRoleException($"user {user.Id} doesn't have role {role}");
    }
}
