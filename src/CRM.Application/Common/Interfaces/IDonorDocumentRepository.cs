namespace CRM.Application.Common.Interfaces;

using CRM.Domain.Entities;
using CRM.Domain.Enums;

public interface IDonorDocumentRepository
{
    /// <summary>Stages a new document row for insert.</summary>
    Task AddAsync(DonorDocument document, CancellationToken cancellationToken = default);

    /// <summary>Tracked list of the donor's active documents of a given type (Upload's supersede step).</summary>
    Task<List<DonorDocument>> GetActiveByDonorAndTypeAsync(
        Guid donorId, DocumentType documentType, CancellationToken cancellationToken = default);

    /// <summary>Tracked document scoped to its donor, for soft-delete.</summary>
    Task<DonorDocument?> GetForMutationAsync(
        Guid donorId, Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>Read-only document scoped to its donor, for download-URL generation.</summary>
    Task<DonorDocument?> GetReadOnlyAsync(
        Guid donorId, Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Just the document's type, or null when no such document exists for that donor.
    /// Used by the per-document-type authorization filter before the download handler runs.
    /// </summary>
    Task<DocumentType?> GetDocumentTypeAsync(
        Guid donorId, Guid documentId, CancellationToken cancellationToken = default);
}
