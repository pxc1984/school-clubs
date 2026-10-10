using System.Buffers.Text;
using System.Security.Cryptography;
using JetBrains.Annotations;
using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Common.Exceptions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Entities.Access;

/// <summary>
/// Диплинк-приглашение: админ отправляет родителю ссылку с <see cref="Token"/>, родитель входит или
/// регистрируется — и ребёнок привязывается к его аккаунту без запроса доступа. Ссылка одноразовая
/// и действует <see cref="Lifetime"/>.
/// </summary>
public class ChildInvite : IAuditableEntity
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    /// <summary>32 случайных байта в base64url без паддинга.</summary>
    public const int TokenLength = 43;

    public ChildInvite(Student student, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(student);

        StudentId = student.Id;
        Token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        ExpiresAt = now + Lifetime;
    }

    [UsedImplicitly]
    private ChildInvite()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid StudentId { get; private set; }

    /// <summary>Секрет из ссылки: знающий его получает доступ к отметкам ребёнка.</summary>
    public string Token { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    /// <summary>Родитель, перешедший по ссылке.</summary>
    public Guid? AcceptedByUserId { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }

    public bool IsActive(DateTimeOffset now)
    {
        return AcceptedAt is null && RevokedAt is null && now < ExpiresAt;
    }

    public bool IsAcceptedBy(Guid userId)
    {
        return AcceptedByUserId == userId;
    }

    /// <exception cref="ChildInviteStateException">Ссылка использована, отозвана или истекла.</exception>
    public void Accept(User parent, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(parent);
        EnsureActive(now);

        AcceptedAt = now;
        AcceptedByUserId = parent.Id;
    }

    public void Revoke(DateTimeOffset now)
    {
        EnsureActive(now);

        RevokedAt = now;
    }

    private void EnsureActive(DateTimeOffset now)
    {
        if (!IsActive(now))
            throw new ChildInviteStateException();
    }
}
