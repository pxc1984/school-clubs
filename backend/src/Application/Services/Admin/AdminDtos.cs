using SchoolClubs.Application.Services.Common;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Services.Admin;

// Группы

public record GroupSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsArchived,
    IReadOnlyList<UserRefDto> Teachers,
    int StudentCount);

public record GroupDetailsDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsArchived,
    IReadOnlyList<UserRefDto> Teachers,
    IReadOnlyList<StudentDto> Students);

public record CreateGroupRequest(string Title, string? Description);

public record UpdateGroupRequest(string Title, string? Description, bool IsArchived);

public record NewStudentRequest(string LastName, string FirstName, string? MiddleName, string? Grade);

/// <summary>Добавить в группу существующих учеников и/или завести новых (массовая вставка списка).</summary>
public record AddGroupStudentsRequest(IReadOnlyList<Guid>? StudentIds, IReadOnlyList<NewStudentRequest>? NewStudents);

// Пользователи

public record UserSummaryDto(
    Guid Id,
    string LastName,
    string FirstName,
    string MiddleName,
    string Email,
    string? Phone,
    IReadOnlyList<UserRole> Roles,
    bool IsBlocked);

public record UserDetailsDto(
    Guid Id,
    string LastName,
    string FirstName,
    string MiddleName,
    string Email,
    string? Phone,
    IReadOnlyList<UserRole> Roles,
    bool IsBlocked,
    DateTimeOffset? ConsentAcceptedAt,
    IReadOnlyList<string> LoginProviders,
    IReadOnlyList<StudentDto> Children,
    IReadOnlyList<GroupRefDto> Groups);

public record SetRolesRequest(IReadOnlyList<UserRole> Roles);

/// <summary>Завести учителя по email: он войдёт первым логином (Яндекс ID с этим email или временный пароль).</summary>
public record InviteTeacherRequest(string Email, string LastName, string FirstName, string? MiddleName);

/// <summary>Показывается админу один раз; пользователь меняет его в профиле.</summary>
public record TemporaryPasswordDto(string Password);

// Ученики

public record StudentListItemDto(
    Guid Id,
    string LastName,
    string FirstName,
    string MiddleName,
    string? Grade,
    IReadOnlyList<GroupRefDto> Groups);

public record StudentDetailsDto(
    Guid Id,
    string LastName,
    string FirstName,
    string MiddleName,
    string? Grade,
    IReadOnlyList<GroupRefDto> Groups,
    IReadOnlyList<UserRefDto> Parents,
    int GradebookEntryCount);

public record CreateStudentRequest(string LastName, string FirstName, string? MiddleName, string? Grade, Guid? GroupId);

public record UpdateStudentRequest(string LastName, string FirstName, string? MiddleName, string? Grade);

public record ParseNamesRequest(string Text);

/// <param name="Line">Номер строки во вставленном тексте, с 1.</param>
/// <param name="Student"><c>null</c>, если строку не удалось разобрать (см. <paramref name="Error"/>).</param>
/// <param name="Matches">Уже существующие ученики с теми же фамилией и именем — возможные дубли.</param>
public record ParsedNameDto(
    int Line,
    string Source,
    NewStudentRequest? Student,
    string? Error,
    IReadOnlyList<StudentDto> Matches);

/// <param name="MovedEntries">Отметки, перенесённые на целевого ученика.</param>
/// <param name="DroppedEntries">Отметки дубля, отброшенные: у целевого ученика уже была отметка по той же задаче.</param>
public record MergeStudentsResultDto(Guid TargetId, int MovedEntries, int DroppedEntries);
