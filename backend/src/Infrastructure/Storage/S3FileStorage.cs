using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;
using SchoolClubs.Application.Common.Interfaces;
using S3Location = SchoolClubs.Domain.Entities.Data.S3Location;

namespace SchoolClubs.Infrastructure.Storage;

public class S3FileStorage(IAmazonS3 s3, IOptions<S3Options> options) : IFileStorage
{
    public S3Location LocationOf(string key)
    {
        return new S3Location(options.Value.Bucket, key);
    }

    public async Task PutAsync(S3Location location,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = location.Bucket,
            Key = location.Key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        }, cancellationToken);
    }

    public async Task<byte[]> ReadAsync(S3Location location, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await s3.GetObjectAsync(location.Bucket, location.Key, cancellationToken);
            using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException(location.ToString(), "file");
        }
    }

    public async Task DeleteAsync(S3Location location, CancellationToken cancellationToken = default)
    {
        await s3.DeleteObjectAsync(location.Bucket, location.Key, cancellationToken);
    }

    public async Task EnsureBucketAsync(CancellationToken cancellationToken = default)
    {
        var bucket = options.Value.Bucket;
        if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3, bucket))
            await s3.PutBucketAsync(bucket, cancellationToken);
    }
}
