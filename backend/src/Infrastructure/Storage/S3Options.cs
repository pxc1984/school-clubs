namespace SchoolClubs.Infrastructure.Storage;

/// <summary>Секция <c>Storage:S3</c>. Локально и в compose — RustFS, в проде — любой S3-совместимый сервис.</summary>
public class S3Options
{
    public const string SectionName = "Storage:S3";

    /// <summary>Адрес S3-совместимого сервиса (<c>http://rustfs:9000</c>); пусто — AWS по <see cref="Region"/>.</summary>
    public string? ServiceUrl { get; set; }

    public string Region { get; set; } = "us-east-1";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string Bucket { get; set; } = "schoolclubs";

    /// <summary>RustFS (как и MinIO) требует path-style адресацию (<c>host/bucket/key</c>).</summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>Создать бакет при старте, если его нет (локальная разработка, тесты).</summary>
    public bool CreateBucket { get; set; } = true;
}
