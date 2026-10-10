using SchoolClubs.Application.Services.Common;

namespace SchoolClubs.Application.Services.ChildInvites;

/// <summary>Ссылку фронт собирает сам: <c>{origin}/invite/{Token}</c>.</summary>
public record ChildInviteDto(Guid Id, string Token, DateTimeOffset Created, DateTimeOffset ExpiresAt);

/// <param name="AlreadyLinked">Ребёнок уже был привязан к этому аккаунту.</param>
public record AcceptedChildInviteDto(StudentDto Student, bool AlreadyLinked);
