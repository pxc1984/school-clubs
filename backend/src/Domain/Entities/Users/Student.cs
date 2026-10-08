using JetBrains.Annotations;

namespace SchoolClubs.Domain.Entities.Users;

/// <summary>
/// Собственно по бизнес требованиям у самих школьников нет аккаунтов в системе, так что они являются
/// абстрактными сущностями Student, которые имеют фио, но не имеют реальной подвязки на клеймы
/// </summary>
public class Student : Profile
{
    public const int GradeMaxLength = 8;

    public Student(string? firstName,
        string? middleName = null,
        string? lastName = null,
        string? grade = null) : base(firstName, middleName, lastName)
    {
        ChangeGrade(grade);
    }

    [UsedImplicitly]
    private Student()
    {
    }

    /// <summary>Класс, например «7Б»: помогает различать тёзок и сопоставлять запросы доступа.</summary>
    public string? Grade { get; private set; }

    public void ChangeGrade(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade))
        {
            Grade = null;
            return;
        }

        var normalized = string.Concat(grade.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
        if (normalized.Length > GradeMaxLength)
            throw new ArgumentException($"grade shouldn't be longer than {GradeMaxLength}", nameof(grade));

        Grade = normalized;
    }
}
