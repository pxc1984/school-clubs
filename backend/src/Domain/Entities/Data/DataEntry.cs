namespace SchoolClubs.Domain.Entities.Data;

public class DataEntry(S3Location location, string originalFileName, string contentType, long sizeBytes)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public S3Location Location { get; private set; } = location;
    public string OriginalFileName { get; private set; } = originalFileName;
    public string ContentType { get; private set; } = contentType;
    public long SizeBytes { get; private set; } = sizeBytes;
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;

    private DataEntry() : this(null!, null!, null!, 0)
    {
    }
}
