using Ardalis.GuardClauses;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Data;
using SchoolClubs.Domain.Entities.Users;
using ValidationException = SchoolClubs.Application.Common.Exceptions.ValidationException;

namespace SchoolClubs.Application.Services.Avatars;

public class AvatarService(
    IUserRepository users,
    IDataEntryRepository dataEntries,
    IFileStorage storage,
    IRemoteFileDownloader downloader,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<AvatarService> logger) : IAvatarService
{
    /// <summary>Префикс ключей в S3: по нему же отличаем аватарки от других файлов (журналов).</summary>
    private const string KeyPrefix = "avatars/";

    public async Task<AvatarDto> UploadAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var user = await GetCurrentUserAsync(cancellationToken);
        var bytes = await ReadLimitedAsync(content, cancellationToken)
                    ?? throw FileError("Файл больше 2 МБ.");
        var avatar = await StoreAsync(user, bytes, cancellationToken)
                     ?? throw FileError("Поддерживаются PNG, JPEG, GIF и WebP.");

        var previous = user.ChangeAvatar(avatar);
        if (previous is not null)
            dataEntries.Remove(previous);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await DeleteFileQuietlyAsync(avatar.Location);
            throw;
        }

        if (previous is not null)
            await DeleteFileQuietlyAsync(previous.Location);
        return new AvatarDto(avatar.Id);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        var previous = user.ChangeAvatar(null);
        if (previous is null)
            return;

        dataEntries.Remove(previous);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await DeleteFileQuietlyAsync(previous.Location);
    }

    public async Task<AvatarFileDto> GetAsync(Guid avatarId, CancellationToken cancellationToken = default)
    {
        var entry = await dataEntries.FindByIdAsync(avatarId);
        // Через этот эндпоинт отдаём только аватарки, а не любые файлы (журналы) по id.
        if (entry is null || !entry.Location.Key.StartsWith(KeyPrefix, StringComparison.Ordinal))
            throw new NotFoundException(avatarId.ToString(), "avatar");

        var content = await storage.ReadAsync(entry.Location, cancellationToken);
        return new AvatarFileDto(entry.ContentType, content);
    }

    public async Task<bool> TryImportAsync(User user, string url, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var bytes = await downloader.DownloadAsync(url, AvatarImages.MaxBytes, cancellationToken);
            if (bytes is null || await StoreAsync(user, bytes, cancellationToken) is not { } avatar)
                return false;

            user.ChangeAvatar(avatar);
            return true;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "Failed to import avatar for user {UserId}", user.Id);
            return false;
        }
    }

    /// <summary>Кладёт картинку в S3 и регистрирует метаданные; <c>null</c>, если формат не поддерживается.</summary>
    private async Task<DataEntry?> StoreAsync(User user, byte[] bytes, CancellationToken cancellationToken)
    {
        if (AvatarImages.Detect(bytes) is not { } format)
            return null;

        var (contentType, extension) = format;
        var location = storage.LocationOf($"{KeyPrefix}{user.Id:N}/{Guid.NewGuid():N}{extension}");
        using (var stream = new MemoryStream(bytes, false))
            await storage.PutAsync(location, stream, contentType, cancellationToken);

        var entry = new DataEntry(location, $"avatar{extension}", contentType, bytes.Length);
        await dataEntries.AddAsync(entry, cancellationToken);
        return entry;
    }

    /// <returns><c>null</c>, если файл больше <see cref="AvatarImages.MaxBytes"/>.</returns>
    private static async Task<byte[]?> ReadLimitedAsync(Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > AvatarImages.MaxBytes)
                return null;
        }

        return buffer.ToArray();
    }

    /// <summary>Файл уже не нужен БД; если S3 недоступен, остаётся сирота — это не повод ронять запрос.</summary>
    private async Task DeleteFileQuietlyAsync(S3Location location)
    {
        try
        {
            await storage.DeleteAsync(location, CancellationToken.None);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to delete avatar file {Location}", location);
        }
    }

    private static ValidationException FileError(string message)
    {
        return new ValidationException([new ValidationFailure("file", message)]);
    }

    /// <exception cref="UnauthorizedAccessException">Нет аккаунта (аноним или сервисный админ).</exception>
    private async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException();

        return await users.GetWithAvatarAsync(userId, cancellationToken) ?? throw new UnauthorizedAccessException();
    }
}
