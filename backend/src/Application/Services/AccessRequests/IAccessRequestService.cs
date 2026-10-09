using SchoolClubs.Domain.Entities.Access;

namespace SchoolClubs.Application.Services.AccessRequests;

/// <summary>Сторона родителя.</summary>
public interface IAccessRequestService
{
    Task<IReadOnlyList<ParentGroupDto>> ListGroupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessRequestDto>> ListMineAsync(CancellationToken cancellationToken = default);

    /// <summary>Не больше 5 ожидающих; повтор на ту же группу и ФИО во время ожидания — 400.</summary>
    Task<AccessRequestDto> CreateAsync(CreateAccessRequestRequest request, CancellationToken cancellationToken = default);

    /// <summary>Отзыв ожидающего запроса.</summary>
    Task<AccessRequestDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Очередь запросов в админке.</summary>
public interface IAdminAccessRequestService
{
    Task<IReadOnlyList<AdminAccessRequestDto>> ListAsync(AccessRequestStatus? status,
        CancellationToken cancellationToken = default);

    Task<AdminAccessRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PendingCountDto> CountPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Одна транзакция: запрос одобрен → ребёнок привязан к родителю (роль Parent выдаётся при необходимости)
    /// → ученик добавлен в группу запроса, если его там нет.
    /// </summary>
    Task<AdminAccessRequestDto> ApproveAsync(Guid id,
        ApproveAccessRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminAccessRequestDto> RejectAsync(Guid id,
        RejectAccessRequestRequest request,
        CancellationToken cancellationToken = default);
}
