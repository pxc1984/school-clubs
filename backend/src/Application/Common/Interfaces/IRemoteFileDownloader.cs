namespace SchoolClubs.Application.Common.Interfaces;

/// <summary>Скачивание файла по внешней ссылке (аватарка от провайдера входа). Реализация — в Infrastructure.</summary>
public interface IRemoteFileDownloader
{
    /// <returns>Содержимое или <c>null</c>: не https, ошибка, ответ не 2xx или файл больше <paramref name="maxBytes"/>.</returns>
    Task<byte[]?> DownloadAsync(string url, int maxBytes, CancellationToken cancellationToken = default);
}
