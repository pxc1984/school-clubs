namespace SchoolClubs.Application.Services.Avatars;

public record AvatarDto(Guid Id);

public record AvatarFileDto(string ContentType, byte[] Content);
