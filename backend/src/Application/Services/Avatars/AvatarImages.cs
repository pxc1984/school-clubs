namespace SchoolClubs.Application.Services.Avatars;

/// <summary>
/// Допустимые форматы аватарок. Тип определяется по сигнатуре файла, а не по имени или заголовку запроса:
/// отдаём файл с этим типом, и подсунуть HTML/SVG под видом картинки нельзя.
/// </summary>
public static class AvatarImages
{
    public const int MaxBytes = 2 * 1024 * 1024;

    private static readonly (string ContentType, string Extension, byte[] Signature, int Offset)[] Formats =
    [
        ("image/png", ".png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], 0),
        ("image/jpeg", ".jpg", [0xFF, 0xD8, 0xFF], 0),
        ("image/gif", ".gif", "GIF8"u8.ToArray(), 0),
        ("image/webp", ".webp", "WEBP"u8.ToArray(), 8),
    ];

    /// <returns>MIME-тип и расширение или <c>null</c>, если формат не поддерживается.</returns>
    public static (string ContentType, string Extension)? Detect(ReadOnlySpan<byte> content)
    {
        foreach (var format in Formats)
        {
            if (content.Length < format.Offset + format.Signature.Length)
                continue;
            if (format.ContentType == "image/webp" && !content.StartsWith("RIFF"u8))
                continue;
            if (content.Slice(format.Offset, format.Signature.Length).SequenceEqual(format.Signature))
                return (format.ContentType, format.Extension);
        }

        return null;
    }
}
