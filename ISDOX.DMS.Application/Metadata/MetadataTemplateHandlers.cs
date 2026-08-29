using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Metadata
{
    public record CreateTemplateCommand(string Name, string Description, List<string> Fields) : IRequest<Guid>;
    public record UpdateTemplateCommand(Guid Id, string Name, string Description, List<string> Fields) : IRequest<bool>;
    public record DeleteTemplateCommand(Guid Id) : IRequest<bool>;
    public record GetAllTemplatesQuery() : IRequest<IEnumerable<MetadataTemplate>>;

    public class MetadataTemplateHandler :
        IRequestHandler<CreateTemplateCommand, Guid>,
        IRequestHandler<UpdateTemplateCommand, bool>,
        IRequestHandler<DeleteTemplateCommand, bool>,
        IRequestHandler<GetAllTemplatesQuery, IEnumerable<MetadataTemplate>>
    {
        private readonly IDmsDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public MetadataTemplateHandler(IDmsDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<Guid> Handle(CreateTemplateCommand request, CancellationToken ct)
        {
            var template = new MetadataTemplate { Name = request.Name, Description = request.Description, AllowedFields = request.Fields };
            _context.MetadataTemplates.Add(template);
            await _context.SaveChangesAsync(ct);
            await _auditLogger.LogAsync(
                        actionType: "Metadata Template Created",
                        entityId: template.Id,
                        entityName: template.Name,
                        // folderPath: template.,
                        status: "Success",
                        ct: ct
                    );
            return template.Id;
        }

        public async Task<bool> Handle(UpdateTemplateCommand request, CancellationToken ct)
        {
            var template = await _context.MetadataTemplates.FindAsync(request.Id);
            if (template == null) return false;
            template.Name = request.Name;
            template.Description = request.Description;
            template.AllowedFields = request.Fields;
            await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
                        actionType: "Metadata Template Updated",
                        entityId: template.Id,
                        entityName: template.Name,
                        // folderPath: template.,
                        status: "Success",
                        ct: ct
                    );
            return true;
        }

        public async Task<bool> Handle(DeleteTemplateCommand request, CancellationToken ct)
        {
            var template = await _context.MetadataTemplates.FindAsync(request.Id);
            if (template == null) return false;
            _context.MetadataTemplates.Remove(template);
            await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
                       actionType: "Metadata Template Deleted",
                       entityId: template.Id,
                       entityName: template.Name,
                       // folderPath: template.,
                       status: "Success",
                       ct: ct
                   );
            return true;
        }

        public async Task<IEnumerable<MetadataTemplate>> Handle(GetAllTemplatesQuery request, CancellationToken ct)
        {
            return await _context.MetadataTemplates.ToListAsync(ct);
        }
    }
}
