namespace SchoolClubs.Application.Services.Common;

public record UserRefDto(Guid Id, string LastName, string FirstName, string MiddleName);

public record StudentDto(Guid Id, string LastName, string FirstName, string MiddleName, string? Grade);

public record GroupRefDto(Guid Id, string Title, bool IsArchived);
