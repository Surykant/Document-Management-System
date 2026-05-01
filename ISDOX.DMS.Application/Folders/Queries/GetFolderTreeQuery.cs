using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Folders.Queries
{
    public record GetFolderTreeQuery() : IRequest<List<FolderNodeDto>>;

    public class GetFolderTreeQueryHandler : IRequestHandler<GetFolderTreeQuery, List<FolderNodeDto>>
    {
        private readonly IDmsDbContext _context;

        public GetFolderTreeQueryHandler(IDmsDbContext context)
        {
            _context = context;
        }

        public async Task<List<FolderNodeDto>> Handle(GetFolderTreeQuery request, CancellationToken ct)
        {
            var allFolders = await _context.Folders
                .OrderBy(f => f.Name) 
                .Select(f => new FolderNodeDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    ParentId = f.ParentId,
                    DocumentCount = f.Documents.Count
                })
                .ToListAsync(ct);

            var nodesMap = allFolders.ToDictionary(f => f.Id);
            var rootNodes = new List<FolderNodeDto>();

            foreach (var folder in allFolders)
            {
                if (folder.ParentId == null)
                {
                    rootNodes.Add(folder);
                }
                else if (nodesMap.TryGetValue(folder.ParentId.Value, out var parentNode))
                {
                    parentNode.Children.Add(folder);
                }
            }

            return rootNodes;
        }
    }
}
