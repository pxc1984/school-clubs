using SchoolClubs.Application.Common.Interfaces;

namespace SchoolClubs.Infrastructure.IntegrationTests.Infrastructure;

/// <summary>Вместо сети: по <see cref="PngUrl"/> отдаёт PNG 1×1, остальное «не скачалось».</summary>
public class FakeRemoteFileDownloader : IRemoteFileDownloader
{
    public const string PngUrl = "https://avatars.test/avatar.png";

    public static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    public Task<byte[]?> DownloadAsync(string url, int maxBytes, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(url == PngUrl ? Png : null);
    }
}
