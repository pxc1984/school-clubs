namespace SchoolClubs.Domain.Entities.Users;

/// <summary>
/// Собственно по бизнес требованиям у самих школьников нет аккаунтов в системе, так что они являются
/// абстрактными сущностями Student, которые имеют фио, но не имеют реальной подвязки на клеймы
/// </summary>
public class Student(string? firstName, string? middleName = null, string? lastName = null)
    : Profile(firstName, middleName, lastName);
