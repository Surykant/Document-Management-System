using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using ISDOX.DMS.Application.Interfaces;

namespace ISDOX.DMS.Infrastructure.Storage;

public class MinioStorageService : IStorageService
{
    private readonly IAmazonS3 _amazonS3;
    private const string BucketName = "documents";

    public MinioStorageService(IAmazonS3 amazonS3)
    {
        _amazonS3 = amazonS3;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        bool bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_amazonS3, BucketName);
        if (!bucketExists)
        {
            var putBucketRequest = new PutBucketRequest { BucketName = BucketName };
            await _amazonS3.PutBucketAsync(putBucketRequest, ct);
        }

        var storageFileName = $"{Guid.NewGuid()}_{fileName}";

        var putRequest = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = storageFileName,
            InputStream = fileStream
        };

        await _amazonS3.PutObjectAsync(putRequest, ct);

        return storageFileName;
    }

    public async Task<Stream> DownloadFileAsync(string fileName, CancellationToken ct)
    {
        var getRequest = new GetObjectRequest
        {
            BucketName = BucketName,
            Key = fileName
        };

        // Reaches into MinIO and pulls the file out as a stream
        var response = await _amazonS3.GetObjectAsync(getRequest, ct);
        return response.ResponseStream;
    }

    public async Task DeleteFileAsync(string fileName, CancellationToken ct)
    {
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = BucketName,
            Key = fileName
        };

        await _amazonS3.DeleteObjectAsync(deleteRequest, ct);
    }
}