using ISDOX.DMS.Domain.Models.Search;

namespace ISDOX.DMS.Application.Interfaces
{
    public interface ISearchService
    {
        Task IndexDocumentAsync(DocumentSearchModel model);

        Task<IEnumerable<DocumentSearchModel>> SearchDocumentsAsync(
            string? query,
            string? owner,
            Guid? folderId,
            DateTime? fromDate,
            DateTime? toDate,
            string? documentType); 
    }
}
