using SchoolClubs.Application.Services.Admin;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Application.Services.Lessons;

namespace SchoolClubs.Application.Services.TeacherGroups;

public interface ITeacherGroupService
{
    /// <summary>Неархивные группы текущего учителя.</summary>
    Task<IReadOnlyList<TeacherGroupCardDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<TeacherGroupDto> GetAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<GroupMatrixDto> GetSummaryAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LessonSummaryDto>> ListLessonsAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<LessonDto> CreateLessonAsync(Guid groupId,
        CreateLessonRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Новый ученик в группе (ФИО + класс). Дубли потом сливает админ.</summary>
    Task<StudentDto> AddStudentAsync(Guid groupId,
        NewStudentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Убирает из состава; отметки остаются.</summary>
    Task RemoveStudentAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken = default);
}
