namespace SchoolClubs.Application.Services.ChildInvites;

/// <summary>Сторона родителя: переход по ссылке.</summary>
public interface IChildInviteService
{
    /// <summary>
    /// Одна транзакция: ссылка помечена использованной → ребёнок привязан к текущему пользователю
    /// (роль Parent выдаётся при необходимости). Повторный переход того же пользователя — не ошибка.
    /// </summary>
    /// <exception cref="Domain.Common.Exceptions.ChildInviteStateException">Ссылка использована другим, отозвана или истекла.</exception>
    Task<AcceptedChildInviteDto> AcceptAsync(string token, CancellationToken cancellationToken = default);
}

/// <summary>Приглашения в карточке ученика в админке.</summary>
public interface IAdminChildInviteService
{
    /// <summary>Действующие ссылки ученика, новые сверху.</summary>
    Task<IReadOnlyList<ChildInviteDto>> ListAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task<ChildInviteDto> CreateAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task RevokeAsync(Guid studentId, Guid inviteId, CancellationToken cancellationToken = default);
}
