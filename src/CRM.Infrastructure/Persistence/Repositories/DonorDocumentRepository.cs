namespace CRM.Infrastructure.Persistence.Repositories;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class DonorDocumentRepository : IDonorDocumentRepository
{
    private readonly CrmDbContext _context;

    public DonorDocumentRepository(CrmDbContext context) => _context = context;

    public Task AddAsync(DonorDocument document, CancellationToken cancellationToken = default)
    {
        _context.DonorDocuments.Add(document);
        return Task.CompletedTask;
    }

    public Task<List<DonorDocument>> GetActiveByDonorAndTypeAsync(
        Guid donorId, DocumentType documentType, CancellationToken cancellationToken = default)
        => _context.DonorDocuments
            .Where(d => d.DonorId == donorId && d.DocumentType == documentType && d.IsActive)
            .ToListAsync(cancellationToken);

    public Task<DonorDocument?> GetForMutationAsync(
        Guid donorId, Guid documentId, CancellationToken cancellationToken = default)
        => _context.DonorDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.DonorId == donorId, cancellationToken);

    public Task<DonorDocument?> GetReadOnlyAsync(
        Guid donorId, Guid documentId, CancellationToken cancellationToken = default)
        => _context.DonorDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.DonorId == donorId, cancellationToken);

    public Task<DocumentType?> GetDocumentTypeAsync(
        Guid donorId, Guid documentId, CancellationToken cancellationToken = default)
        => _context.DonorDocuments
            .AsNoTracking()
            .Where(d => d.Id == documentId && d.DonorId == donorId)
            .Select(d => (DocumentType?)d.DocumentType)
            .FirstOrDefaultAsync(cancellationToken);
}
