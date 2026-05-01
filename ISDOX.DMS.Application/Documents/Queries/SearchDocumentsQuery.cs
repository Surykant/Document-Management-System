using Elastic.Clients.Elasticsearch;
using MediatR;

namespace ISDOX.DMS.Application.Documents.Queries
{
    public class DocumentIndexModel
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public record DocumentSearchResult(Guid DocumentId, string FileName, string Content);

    public record SearchDocumentsQuery(string Keyword) : IRequest<List<DocumentSearchResult>>;

    public class SearchDocumentsQueryHandler : IRequestHandler<SearchDocumentsQuery, List<DocumentSearchResult>>
    {
        private readonly ElasticsearchClient _elasticClient;

        public SearchDocumentsQueryHandler(ElasticsearchClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public async Task<List<DocumentSearchResult>> Handle(SearchDocumentsQuery request, CancellationToken ct)
        {
            var response = await _elasticClient.SearchAsync<DocumentIndexModel>(s => s
                .Indices("documents")
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.Content)
                        .Query(request.Keyword)
                        .Fuzziness(new Fuzziness(1)) 
                    )
                ), ct);

            if (!response.IsValidResponse)
            {
                throw new Exception($"ELASTICSEARCH ERROR: {response.DebugInformation}");
            }

            if (response.Documents.Count == 0)
            {
                return new List<DocumentSearchResult>();
            }

            return response.Documents.Select(d => new DocumentSearchResult(
                d.DocumentId,
                d.FileName,
                d.Content.Length > 200 ? d.Content.Substring(0, 200).Replace("\n", " ").Replace("\r", "") + "..." : d.Content
            )).ToList();
        }
    }
}
