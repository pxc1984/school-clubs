using System.Text.Json.Serialization;

namespace SchoolClubs.Domain.Entities.Users;

/// <summary>Пол пользователя. Не указан — <c>null</c> в <see cref="User.Gender"/>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Gender>))]
public enum Gender
{
    Male,
    Female,
}
