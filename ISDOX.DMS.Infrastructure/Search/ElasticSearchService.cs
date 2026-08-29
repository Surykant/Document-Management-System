using DocumentFormat.OpenXml.Presentation;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Models.Search;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            // Use the explicit request descriptor to guarantee v8 understands the index and ID
            var response = await _client.IndexAsync(model, req => req
                .Index(IndexName)
                .Id(model.Id.ToString()) // CRITICAL: Forces ES to use your Postgres Guid instead of a random str   ing!
            );

            if (!response.IsValidResponse)
            {
                Console.WriteLine($"Failed to index document: {response.DebugInformation}");
            }
        }
        
        public async Task<IEnumerable<DocumentSearchModel>> SearchDocumentsAsync(
            string? query, string? owner, Guid? folderId,
            DateTime? fromDate, DateTime? toDate, string? documentType)
        {
            // 1. MUST CLAUSES (The Keyword Search)
            var mustQueries = new List<Query>();
            if (!string.IsNullOrWhiteSpace(query) && query != "*")
            {
                mustQueries.Add(new MultiMatchQuery
                {
                    Query = query,
                    // Searching across Name, Description, and Tags
                    Fields = new[] { "name", "description", "tags" },
                    Fuzziness = new Fuzziness("AUTO")
                });
            }
            else
            {
                mustQueries.Add(new MatchAllQuery());
            }

            // 2. FILTER CLAUSES (Exact Matches & Ranges)
            var filterQueries = new List<Query>();

            if (folderId.HasValue)
            {
                filterQueries.Add(new MatchQuery { Field = "folderId", Query = folderId.Value.ToString() });
            }

            if (!string.IsNullOrWhiteSpace(owner))
            {
                filterQueries.Add(new MatchQuery { Field = "owner", Query = owner });
            }

            // NEW: Document Type / File Extension Filter
            if (!string.IsNullOrWhiteSpace(documentType))
            {
                var cleanExtension = documentType.Replace(".", "").ToLowerInvariant();

                // Create a nested OR (Should) query so it checks both fields
                filterQueries.Add(new BoolQuery
                {
                    Should = new List<Query>
                    {
                        // Check the actual extension field (for newly uploaded documents)
                        new WildcardQuery { Field = "fileExtension", Value = $"*{cleanExtension}*" },
                        
                        // Fallback: Check the document Name (for old documents where fileExtension is missing)
                        new WildcardQuery { Field = "name", Value = $"*{cleanExtension}*" }
                    },
                    MinimumShouldMatch = 1 // It must match at least one of the above
                });
            }

            if (fromDate.HasValue || toDate.HasValue)
            {
                var dateRange = new DateRangeQuery { Field = "createdAt" };

                if (fromDate.HasValue) dateRange.Gte = fromDate.Value;
                if (toDate.HasValue) dateRange.Lte = toDate.Value;

                filterQueries.Add(dateRange);
            }

            // 3. EXECUTE QUERY
            var response = await _client.SearchAsync<DocumentSearchModel>(s => s
                .Indices(IndexName)
                .From(0)
                .Size(100)
                .Query(q => q
                    .Bool(b => b
                        .Must(mustQueries)
                        .Filter(filterQueries)
                    )
                )
            );

            if (!response.IsValidResponse)
            {
                Console.WriteLine($"[Elasticsearch Error]: {response.DebugInformation}");
                return Array.Empty<DocumentSearchModel>();
            }

            return response.Documents;
        }
    }
}