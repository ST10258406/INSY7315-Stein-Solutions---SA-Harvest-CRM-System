namespace CRM.Application.Modules.Dashboard.Queries.GetManagerActivity;

using CRM.Application.Modules.Dashboard.Dtos;
using MediatR;

/// <summary>Donors contacted per user over a rolling window. Period is "weekly" or "monthly".</summary>
public record GetManagerActivityQuery(string Period) : IRequest<ManagerActivityDto>;
