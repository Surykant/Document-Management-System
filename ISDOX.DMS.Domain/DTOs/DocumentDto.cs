namespace ISDOX.DMS.Domain.DTOs
{
    public record DocumentDto(
         Guid Id,
         string Name,
         string Description,
         Guid? FolderId,
         string Owner,
         DateTime CreatedAt,
         Dictionary<string, string>? CustomMetadata,
         int VersionNumber,
         string FileExtension,
         string StoragePath,
         string? TemplateName,
         string? FolderName,
         string? DocumentSize
     );
}
