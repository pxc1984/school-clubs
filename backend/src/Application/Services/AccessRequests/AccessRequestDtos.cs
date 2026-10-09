using SchoolClubs.Application.Services.Admin;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Domain.Entities.Access;

namespace SchoolClubs.Application.Services.AccessRequests;

/// <summary>Активная группа для формы запроса — без состава.</summary>
public record ParentGroupDto(Guid Id, string Title, string? Description);

/// <param name="Relation">
/// <c>null</c> — мама или папа по полу из профиля; если пол не указан, обязателен.
/// </param>
/// <param name="AcceptChildDataConsent">Чекбокс «Я законный представитель и согласен на обработку данных ребёнка».</param>
public record CreateAccessRequestRequest(
    Guid GroupId,
    string ChildLastName,
    string ChildFirstName,
    string? ChildMiddleName,
    string? Grade,
    ParentRelation? Relation,
    string? Comment,
    bool AcceptChildDataConsent);

/// <summary>Запрос глазами родителя.</summary>
public record AccessRequestDto(
    Guid Id,
    GroupRefDto Group,
    string ChildLastName,
    string ChildFirstName,
    string? ChildMiddleName,
    string? Grade,
    ParentRelation Relation,
    string? Comment,
    AccessRequestStatus Status,
    string? RejectReason,
    DateTimeOffset Created,
    DateTimeOffset? DecidedAt);

public record RequestParentDto(
    Guid Id,
    string LastName,
    string FirstName,
    string MiddleName,
    string Email,
    string? Phone,
    IReadOnlyList<StudentDto> Children);

/// <summary>Карточка запроса в админке.</summary>
/// <param name="Student">С кем сопоставлен при одобрении.</param>
/// <param name="Suggestions">Похожие ученики из указанной группы — кандидаты для одобрения.</param>
public record AdminAccessRequestDto(
    Guid Id,
    RequestParentDto Parent,
    GroupRefDto Group,
    string ChildLastName,
    string ChildFirstName,
    string? ChildMiddleName,
    string? Grade,
    ParentRelation Relation,
    string? Comment,
    AccessRequestStatus Status,
    string? RejectReason,
    DateTimeOffset Created,
    DateTimeOffset? DecidedAt,
    StudentDto? Student,
    IReadOnlyList<StudentDto> Suggestions);

/// <summary>Ровно одно из двух: существующий ученик или новый (он будет добавлен в группу запроса).</summary>
public record ApproveAccessRequestRequest(Guid? StudentId, NewStudentRequest? NewStudent);

public record RejectAccessRequestRequest(string Reason);

public record PendingCountDto(int Count);
