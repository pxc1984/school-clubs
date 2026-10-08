namespace SchoolClubs.Application.Services.Admin;

public interface IAdminStudentService
{
    Task<IReadOnlyList<StudentListItemDto>> SearchAsync(string? query,
        Guid? groupId,
        CancellationToken cancellationToken = default);

    Task<StudentDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StudentDetailsDto> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken = default);

    Task<StudentDetailsDto> UpdateAsync(Guid id,
        UpdateStudentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Только ученика без отметок; дубль с отметками сливают, данные по заявлению — стирают.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Сливает дубль в целевого ученика: отметки, группы, родители; дубль удаляется.</summary>
    Task<MergeStudentsResultDto> MergeAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default);

    /// <summary>Разбор вставленного столбика ФИО с подсказкой возможных дублей — для предпросмотра.</summary>
    Task<IReadOnlyList<ParsedNameDto>> ParseNamesAsync(ParseNamesRequest request,
        CancellationToken cancellationToken = default);
}
