using Elastic.Clients.Elasticsearch;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Models.Search;

namespace ISDOX.DMS.Infrastructure.Search
{
    public class ElasticSearchService : ISearchService
    {
        private readonly ElasticsearchClient _client;
        private const string IndexName = "isdox-documents";

        public ElasticSearchService(ElasticsearchClient client)
        {
            _client = client;
        }

        public async Task IndexDocumentAsync(DocumentSearchModel model)
        {
            // V8 Client uses a cleaner IndexAsync method
            var response = await _client.IndexAsync(model, IndexName);

            if (!response.IsValidResponse)
            {
                Console.WriteLine($"Failed to index document: {response.DebugInformation}");
            }
        }

        public async Task<IEnumerable<DocumentSearchModel>> SearchDocumentsAsync(
    string? query,
    string? owner,
    Guid? folderId,
    DateTime? fromDate,
    DateTime? toDate)
        {
            var response = await _client.SearchAsync<DocumentSearchModel>(s => s
                .Indices(IndexName)
                .From(0)
                .Size(100)
                .Query(q => q
                    .Bool(b => b
                        .Must(m => {
                            if (!string.IsNullOrEmpty(query) && query != "*")
                                m.QueryString(qs => qs.Query(query));
                            else
                                m.MatchAll();
                        })
                        .Filter(f => {
                            if (folderId.HasValue)
                                f.Term(t => t.Field(ff => ff.FolderId).Value(folderId.Value.ToString()));

                            if (!string.IsNullOrEmpty(owner))
                                f.Term(t => t.Field(ff => ff.Owner).Value(owner));

                            if (fromDate.HasValue || toDate.HasValue)
                                f.Range(r => r
                                    .Date(dr => dr
                                        .Field(ff => ff.CreatedAt)
                                        .Gte(fromDate)
                                        .Lte(toDate)));
                        })
                    )
                )
            );

            return response.Documents;
        }
    }
}
