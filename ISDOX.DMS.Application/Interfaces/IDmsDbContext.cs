using ISDOX.DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Document = ISDOX.DMS.Domain.Entities.Document;

namespace ISDOX.DMS.Application.Interfaces
{
    public interface IDmsDbContext
    {
        DbSet<Document> Documents { get; }
        DbSet<DocumentVersion> DocumentVersions { get; }
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        DbSet<UserRole> UserRoles { get; }
        DbSet<Folder> Folders { get; }
        DbSet<RefreshToken> RefreshTokens { get; set; }
        DbSet<Permission> Permissions { get; set; }
        DbSet<RolePermission> RolePermissions { get; set; }
        DbSet<MetadataTemplate> MetadataTemplates { get; set; }
        DbSet<BulkImportJob> BulkImportJobs { get; set; }

        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
