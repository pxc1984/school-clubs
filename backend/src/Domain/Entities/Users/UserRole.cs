namespace SchoolClubs.Domain.Entities.Users;

/// <summary>Роли комбинируются свободно: учитель-админ может быть ещё и родителем.</summary>
public enum UserRole
{
    Parent,
    Teacher,
    Admin,
}
