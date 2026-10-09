using SchoolClubs.Domain.Entities.Data;

namespace SchoolClubs.Application.Common.Interfaces;

/// <summary>Объектное хранилище файлов (S3 / MinIO). Реализация — в Infrastructure.</summary>
public interface IFileStorage
{
    /// <summary>Где окажется файл с этим ключом (бакет берётся из конфигурации).</summary>
    S3Location LocationOf(string key);

    Task PutAsync(S3Location location, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Содержимое целиком в памяти: файлы журналов маленькие (до 2 МБ).</summary>
    Task<byte[]> ReadAsync(S3Location location, CancellationToken cancellationToken = default);

    Task DeleteAsync(S3Location location, CancellationToken cancellationToken = default);
}
