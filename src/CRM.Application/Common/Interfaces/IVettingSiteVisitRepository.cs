namespace CRM.Application.Common.Interfaces;

using CRM.Application.Modules.Vetting.SiteVisits.Dtos;
using CRM.Domain.Entities.Vetting;

public interface IVettingSiteVisitRepository
{
    /// <summary>One day's visits, optionally for one officer, ordered by scheduled time.</summary>
    Task<List<VettingSiteVisitDto>> GetByDateAsync(DateOnly date, Guid? officerId, CancellationToken cancellationToken = default);

    Task<VettingSiteVisitDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Stages a new visit for insert. Commit with <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    Task AddAsync(VettingSiteVisit visit, CancellationToken cancellationToken = default);

    /// <summary>Tracked visit for the PATCH handler, or null.</summary>
    Task<VettingSiteVisit?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}
