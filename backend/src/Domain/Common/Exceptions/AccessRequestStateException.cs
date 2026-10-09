using SchoolClubs.Domain.Entities.Access;

namespace SchoolClubs.Domain.Common.Exceptions;

/// <summary>Запрос уже обработан: из финального статуса переходов нет.</summary>
public class AccessRequestStateException(AccessRequestStatus status)
    : InvalidOperationException($"access request is already {status}")
{
    public AccessRequestStatus Status { get; } = status;
}
