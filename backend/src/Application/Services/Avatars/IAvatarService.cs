using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Services.Avatars;

public interface IAvatarService
{
    /// <summary>Загружает аватарку текущего пользователя вместо прежней.</summary>
    /// <exception cref="Common.Exceptions.ValidationException">Не PNG/JPEG/GIF/WebP или больше 2 МБ.</exception>
    Task<AvatarDto> UploadAsync(Stream content, CancellationToken cancellationToken = default);

    /// <summary>Удаляет аватарку текущего пользователя (и файл в S3).</summary>
    Task DeleteAsync(CancellationToken cancellationToken = default);

    Task<AvatarFileDto> GetAsync(Guid avatarId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Скачивает картинку провайдера входа и делает её аватаркой <paramref name="user"/>; изменения сохраняет
    /// вызывающий. Ошибки не бросает: без аватарки вход всё равно должен пройти.
    /// </summary>
    Task<bool> TryImportAsync(User user, string url, CancellationToken cancellationToken = default);
}
