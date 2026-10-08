using JetBrains.Annotations;

namespace SchoolClubs.Domain.Entities.Users;

/// <summary>
/// Выданная пользователю роль. Отдельный тип нужен только для хранения ролей строками таблицы user_role;
/// снаружи роли видны как <see cref="User.Roles"/>.
/// </summary>
public sealed class UserRoleGrant
{
    internal UserRoleGrant(UserRole role)
    {
        Role = role;
    }

    [UsedImplicitly]
    private UserRoleGrant()
    {
    }

    public UserRole Role { get; private set; }
}
