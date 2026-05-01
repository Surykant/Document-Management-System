using ISDOX.DMS.Application.Events;
using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISDOX.DMS.Application.Documents.Commands
{
    public record DeleteDocumentCommand(Guid DocumentId) : IRequest<bool>;

    public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand, bool>
    {
        private readonly IDmsDbContext _context;
        private readonly IStorageService _storage;
        private readonly IMessagePublisher _messagePublisher;

        public DeleteDocumentCommandHandler(IDmsDbContext context, IStorageService storage, IMessagePublisher messagePublisher)
        {
            _context = context;
            _storage = storage;
            _messagePublisher = messagePublisher;
        }

        public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken ct)
        {
            var document = await _context.Documents
                .Include(d => d.Versions)
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId, ct);

            if (document == null) return false;

            foreach (var version in document.Versions)
            {
                await _storage.DeleteFileAsync(version.StoragePath, ct);
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync(ct);

            await _messagePublisher.PublishAsync(new DocumentDeletedEvent(document.Id), ct);

            return true;
        }
    }
}
