namespace ISDOX.DMS.Application.Events
{
    public record DocumentUploadedEvent(
    Guid DocumentId,
    string FileName,
    string StoragePath,
    string Owner, 
    DateTime CreatedAt, 
    Dictionary<string, string>? Metadata
);
}
