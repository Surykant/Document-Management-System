namespace ISDOX.DMS.Application.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, CancellationToken ct);

        Task<Stream> DownloadFileAsync(string storagePath, CancellationToken ct);

        Task DeleteFileAsync(string storagePath, CancellationToken ct);
    }
}
