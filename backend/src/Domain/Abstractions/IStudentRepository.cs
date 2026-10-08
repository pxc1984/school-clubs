using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Abstractions;

public interface IStudentRepository : IRepository<Student>
{
    Task<IReadOnlyList<Student>> ListByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>Поиск по словам (подстроки фамилии, имени, отчества), опционально — только в группе.</summary>
    Task<IReadOnlyList<Student>> SearchAsync(string? query,
        Guid? groupId,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>Тёзки: совпадение фамилии и имени без учёта регистра.</summary>
    Task<IReadOnlyList<Student>> ListByNameAsync(string lastName,
        string firstName,
        CancellationToken cancellationToken = default);

    Task<int> CountGradebookEntriesAsync(Guid studentId, CancellationToken cancellationToken = default);
}
