using System.Text.Json.Serialization;
using JetBrains.Annotations;
using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Common.Exceptions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Entities.Access;

[JsonConverter(typeof(JsonStringEnumConverter<AccessRequestStatus>))]
public enum AccessRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
}

/// <summary>Кем родитель приходится ребёнку.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ParentRelation>))]
public enum ParentRelation
{
    Mother,
    Father,
    Guardian,
    Other,
}

public static class ParentRelations
{
    /// <summary>Мама или папа по полу родителя; пол не указан — <c>null</c>.</summary>
    public static ParentRelation? FromGender(Gender? gender)
    {
        return gender switch
        {
            Gender.Female => ParentRelation.Mother,
            Gender.Male => ParentRelation.Father,
            _ => null,
        };
    }
}

/// <summary>
/// Запрос родителя на доступ к отметкам ребёнка. Родитель не знает id ученика, поэтому описывает ребёнка
/// словами, а сопоставляет с учеником админ при одобрении. Из <see cref="AccessRequestStatus.Pending"/>
/// три финальных выхода: одобрение, отказ, отзыв.
/// </summary>
public class AccessRequest : IAuditableEntity
{
    public const int CommentMaxLength = 1000;
    public const int RejectReasonMaxLength = 500;

    public AccessRequest(User parent,
        Guid groupId,
        string childLastName,
        string childFirstName,
        string? childMiddleName,
        string? grade,
        ParentRelation relation,
        string? comment,
        DateTimeOffset consentAcceptedAt)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentException.ThrowIfNullOrWhiteSpace(childLastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(childFirstName);
        if (groupId == Guid.Empty)
            throw new ArgumentException("group id is empty", nameof(groupId));
        if (!Enum.IsDefined(relation))
            throw new ArgumentOutOfRangeException(nameof(relation), relation, "unknown relation");
        var trimmedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (trimmedComment?.Length > CommentMaxLength)
            throw new ArgumentException($"comment shouldn't be longer than {CommentMaxLength}", nameof(comment));

        UserId = parent.Id;
        GroupId = groupId;
        ChildLastName = childLastName.Trim();
        ChildFirstName = childFirstName.Trim();
        ChildMiddleName = string.IsNullOrWhiteSpace(childMiddleName) ? null : childMiddleName.Trim();
        Grade = Student.NormalizeGrade(grade);
        Relation = relation;
        Comment = trimmedComment;
        ConsentAcceptedAt = consentAcceptedAt;
    }

    [UsedImplicitly]
    private AccessRequest()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Родитель, подавший запрос.</summary>
    public Guid UserId { get; private set; }

    public Guid GroupId { get; private set; }

    public string ChildLastName { get; private set; } = string.Empty;
    public string ChildFirstName { get; private set; } = string.Empty;
    public string? ChildMiddleName { get; private set; }
    public string? Grade { get; private set; }
    public ParentRelation Relation { get; private set; }
    public string? Comment { get; private set; }

    /// <summary>Чекбокс «Я законный представитель и согласен на обработку данных ребёнка».</summary>
    public DateTimeOffset ConsentAcceptedAt { get; private set; }

    public AccessRequestStatus Status { get; private set; } = AccessRequestStatus.Pending;

    /// <summary>Ученик, с которым админ сопоставил запрос при одобрении.</summary>
    public Guid? StudentId { get; private set; }

    /// <summary>Причина отказа — её видит родитель.</summary>
    public string? RejectReason { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public bool IsPending
    {
        get { return Status == AccessRequestStatus.Pending; }
    }

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }

    public void Approve(Student student, DateTimeOffset decidedAt)
    {
        ArgumentNullException.ThrowIfNull(student);
        EnsurePending();

        Status = AccessRequestStatus.Approved;
        StudentId = student.Id;
        DecidedAt = decidedAt;
    }

    public void Reject(string reason, DateTimeOffset decidedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var trimmed = reason.Trim();
        if (trimmed.Length > RejectReasonMaxLength)
            throw new ArgumentException($"reason shouldn't be longer than {RejectReasonMaxLength}", nameof(reason));
        EnsurePending();

        Status = AccessRequestStatus.Rejected;
        RejectReason = trimmed;
        DecidedAt = decidedAt;
    }

    /// <summary>Отзыв родителем.</summary>
    public void Cancel(DateTimeOffset decidedAt)
    {
        EnsurePending();

        Status = AccessRequestStatus.Cancelled;
        DecidedAt = decidedAt;
    }

    /// <summary>Тот же ребёнок в той же группе — для запрета дублей среди ожидающих.</summary>
    public bool IsSameChild(Guid groupId, string lastName, string firstName)
    {
        return GroupId == groupId
               && string.Equals(Normalize(ChildLastName), Normalize(lastName), StringComparison.Ordinal)
               && string.Equals(Normalize(ChildFirstName), Normalize(firstName), StringComparison.Ordinal);
    }

    private void EnsurePending()
    {
        if (!IsPending)
            throw new AccessRequestStateException(Status);
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant().Replace('ё', 'е');
    }
}
