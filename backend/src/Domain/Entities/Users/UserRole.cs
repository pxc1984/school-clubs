using System.Text.Json.Serialization;

namespace SchoolClubs.Domain.Entities.Users;

/// <summary>Роли комбинируются свободно: учитель-админ может быть ещё и родителем.</summary>
/// <remarks>В JSON — строкой (<c>"Teacher"</c>), как и в клеймах сессии.</remarks>
[JsonConverter(typeof(JsonStringEnumConverter<UserRole>))]
public enum UserRole
{
    Parent,
    Teacher,
    Admin,
}
