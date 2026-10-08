namespace SchoolClubs.Domain.Entities.Data;

public record S3Location
{
    public S3Location(string bucket, string key)
    {
        if (string.IsNullOrWhiteSpace(bucket))
            throw new ArgumentException("bucket is required", nameof(bucket));
        if (string.IsNullOrWhiteSpace(key) || key.StartsWith('/'))
            throw new ArgumentException("key must be non-empty and not start with '/'", nameof(key));

        Bucket = bucket;
        Key = key;
    }

    public string Bucket { get; }
    public string Key { get; }

    public override string ToString()
    {
        return $"s3://{Bucket}/{Key}";
    }
}
