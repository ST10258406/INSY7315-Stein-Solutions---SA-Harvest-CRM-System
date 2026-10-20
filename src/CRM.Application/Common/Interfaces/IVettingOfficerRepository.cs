namespace CRM.Application.Common.Interfaces;

using CRM.Application.Modules.Vetting.Officers.Dtos;
using CRM.Domain.Entities.Vetting;

public interface IVettingOfficerRepository
{
    /// <summary>Every officer, by name. Small table that fills the officer dropdown, so not paged.</summary>
    Task<List<VettingOfficerDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<VettingOfficerDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Stages a new officer for insert. Commit with <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    Task AddAsync(VettingOfficer officer, CancellationToken cancellationToken = default);

    /// <summary>Tracked officer for the PATCH handler, or null.</summary>
    Task<VettingOfficer?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets workload_count to (site visits not Completed) + (submissions in New, Ready or
    /// Returned), recounted from the database, never ±1. Counts only SAVED rows, so save the
    /// change first and call this inside the same <see cref="IUnitOfWork.ExecuteInTransactionAsync{T}"/>,
    /// then save again.
    /// </summary>
    Task RecalculateWorkloadAsync(Guid officerId, CancellationToken cancellationToken = default);
}
