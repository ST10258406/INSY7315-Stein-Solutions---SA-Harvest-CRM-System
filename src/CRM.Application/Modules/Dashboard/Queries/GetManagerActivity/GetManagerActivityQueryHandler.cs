namespace CRM.Application.Modules.Dashboard.Queries.GetManagerActivity;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Dashboard.Dtos;
using MediatR;

public class GetManagerActivityQueryHandler : IRequestHandler<GetManagerActivityQuery, ManagerActivityDto>
{
    public const int MaxManagers = 6;

    private readonly IDashboardRepository _dashboard;

    public GetManagerActivityQueryHandler(IDashboardRepository dashboard)
    {
        _dashboard = dashboard;
    }

    public async Task<ManagerActivityDto> Handle(GetManagerActivityQuery request, CancellationToken cancellationToken)
    {
        var monthly = string.Equals(request.Period, "monthly", StringComparison.OrdinalIgnoreCase);
        var to = DateTime.UtcNow;
        var from = to.AddDays(monthly ? -30 : -7);

        var rows = await _dashboard.GetDonorsContactedByUserAsync(from, to, MaxManagers, cancellationToken);

        return new ManagerActivityDto
        {
            Period = monthly ? "monthly" : "weekly",
            FromUtc = from,
            ToUtc = to,
            Items = rows
                .Select(r => new ManagerActivityItemDto
                {
                    UserId = r.UserId,
                    Name = $"{r.FirstName} {r.LastName}".Trim(),
                    DonorsContacted = r.DonorsContacted
                })
                .ToList()
        };
    }
}
