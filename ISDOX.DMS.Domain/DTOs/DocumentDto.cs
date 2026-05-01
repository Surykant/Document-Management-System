namespace ISDOX.DMS.Domain.DTOs
{
    public record DocumentDto(
    Guid Id,
    string Name,
    string Description,
    Guid? FolderId,
    string Owner,
    DateTime CreatedAt,
    Dictionary<string, string> CustomMetadata,
    int LatestVersionNumber,
    string FileExtension,
    string StoragePath);
}
