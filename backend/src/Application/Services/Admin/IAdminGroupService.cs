namespace SchoolClubs.Application.Services.Admin;

public interface IAdminGroupService
{
    Task<IReadOnlyList<GroupSummaryDto>> ListAsync(bool includeArchived, CancellationToken cancellationToken = default);
    Task<GroupDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GroupDetailsDto> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);
    Task<GroupDetailsDto> UpdateAsync(Guid id, UpdateGroupRequest request, CancellationToken cancellationToken = default);

    /// <summary>Только группу без занятий; прошлогоднюю группу архивируют.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GroupDetailsDto> AddTeacherAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);
    Task<GroupDetailsDto> RemoveTeacherAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

    Task<GroupDetailsDto> AddStudentsAsync(Guid groupId,
        AddGroupStudentsRequest request,
        CancellationToken cancellationToken = default);

    Task<GroupDetailsDto> RemoveStudentAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken = default);
}
