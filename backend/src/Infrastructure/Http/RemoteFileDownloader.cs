using Microsoft.Extensions.Logging;
using SchoolClubs.Application.Common.Interfaces;

namespace SchoolClubs.Infrastructure.Http;

public class RemoteFileDownloader(HttpClient httpClient, ILogger<RemoteFileDownloader> logger) : IRemoteFileDownloader
{
    public async Task<byte[]?> DownloadAsync(string url, int maxBytes, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return null;

        try
        {
            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > maxBytes)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                buffer.Write(chunk, 0, read);
                if (buffer.Length > maxBytes)
                    return null;
            }

            return buffer.ToArray();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException
                                      && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(e, "Failed to download {Url}", uri);
            return null;
        }
    }
}
