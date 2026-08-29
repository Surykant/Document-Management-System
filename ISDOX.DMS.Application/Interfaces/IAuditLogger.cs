namespace ISDOX.DMS.Application.Interfaces
{
    public interface IAuditLogger
    {
        Task LogAsync(
         string actionType,
         Guid? entityId = null,
         string? entityName = null,
         string? folderPath = null,
         string status = "Success",
         string? overrideUserId = null,    
         string? overrideUserEmail = null,
         CancellationToken ct = default);
    }
}