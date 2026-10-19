namespace CRM.Application.Modules.Agent.Tools;

using System.ComponentModel;
using System.Text.RegularExpressions;
using CRM.Application.Common.Exceptions;
using CRM.Application.Modules.Approvals.Queries.GetApprovals;
using CRM.Application.Modules.Dashboard.Queries.GetDashboardStats;
using CRM.Application.Modules.Dashboard.Queries.GetManagerActivity;
using CRM.Application.Modules.Donors.Queries.GetDonorById;
using CRM.Application.Modules.Donors.Queries.GetDonors;
using CRM.Application.Modules.Interactions.Queries.GetDonorInteractions;
using CRM.Application.Modules.Lookups.Queries.GetRelationshipManagers;
using CRM.Application.Modules.Reports.Queries.GetDonorsByRegionReport;
using CRM.Application.Modules.Reports.Queries.GetDonorsByStatusReport;
using CRM.Application.Modules.Reports.Queries.GetDonorsByTypeReport;
using CRM.Application.Modules.Reports.Queries.GetDonorsContactedReport;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Application.Modules.Tasks.Queries.GetDonorTasks;
using CRM.Application.Modules.Tasks.Queries.GetMyTasks;
using MediatR;
using Microsoft.Extensions.AI;

/// <summary>
/// Read-only tools the CRM agent may call (docs/agent-spec.md §6). Each tool is a thin
/// wrapper over an existing MediatR query and returns an explicit allow-listed projection,
/// never a DTO as-is unless it is aggregate-only. Only wrap queries whose controller
/// endpoint policy includes SuperAdmin (§4.2).
///
/// Data minimisation (POPIA, US-5): no result may contain contact phone numbers or email
/// addresses, tax or company registration numbers, document file names or any URL.
/// Free-text fields are passed through <see cref="Scrub"/> because people type those in.
/// </summary>
public sealed partial class CrmAgentTools(ISender sender)
{
    private const int PageSize = 25;
    private const int InteractionLimit = 20;
    private const int MaxSummaryLength = 500;

    public IList<AITool> AsTools() =>
    [
        AIFunctionFactory.Create(SearchDonorsAsync, "search_donors"),
        AIFunctionFactory.Create(GetDonorAsync, "get_donor"),
        AIFunctionFactory.Create(GetDonorInteractionsAsync, "get_donor_interactions"),
        AIFunctionFactory.Create(GetDonorTasksAsync, "get_donor_tasks"),
        AIFunctionFactory.Create(GetMyTasksAsync, "get_my_tasks"),
        AIFunctionFactory.Create(ListApprovalsAsync, "list_approvals"),
        AIFunctionFactory.Create(GetDashboardStatsAsync, "get_dashboard_stats"),
        AIFunctionFactory.Create(GetManagerActivityAsync, "get_manager_activity"),
        AIFunctionFactory.Create(RunReportAsync, "run_report"),
        AIFunctionFactory.Create(ListRelationshipManagersAsync, "list_relationship_managers"),
    ];

    [Description("Search donors. Returns at most 25 per page plus totalCount.")]
    public Task<object> SearchDonorsAsync(
        [Description("Free text matched against the company name.")] string? search = null,
        [Description("One of: PendingReview, Active, Lapsed, Rejected.")] string? status = null,
        [Description("Operational region code, e.g. GP, WC, KZN.")] string? regionCode = null,
        [Description("Relationship manager id from list_relationship_managers.")] Guid? relationshipManagerId = null,
        [Description("Only donors with a follow-up date before this date (yyyy-MM-dd).")] DateOnly? followUpBefore = null,
        [Description("1-based page number.")] int page = 1,
        CancellationToken cancellationToken = default) =>
        RunAsync(async () =>
        {
            var result = await sender.Send(new GetDonorsQuery
            {
                Search = search,
                Status = status,
                RegionCode = regionCode,
                RelationshipManagerId = relationshipManagerId,
                FollowUpBefore = followUpBefore,
                Page = page,
                PageSize = PageSize,
            }, cancellationToken);

            return new
            {
                result.Pagination.TotalCount,
                Items = result.Data.Select(d => new
                {
                    d.Id,
                    d.CompanyName,
                    d.CompanyType,
                    d.Status,
                    RelationshipManager = d.RelationshipManager?.FullName,
                    d.FollowUpDate,
                    d.LastInteractionDate,
                    d.LastInteractionType,
                    d.OperationalRegions,
                    d.DonationTypes,
                    d.DonationFrequency,
                }).ToList(),
            };
        });

    [Description("Get one donor's profile by id (from search_donors): company, primary contact name and job title, donation details, relationship manager, follow-up date, B-BBEE status and document count. Contact details are never available.")]
    public Task<object> GetDonorAsync(
        [Description("Donor id.")] Guid donorId,
        CancellationToken cancellationToken = default) =>
        RunAsync(async () =>
        {
            var d = await sender.Send(new GetDonorByIdQuery(donorId), cancellationToken);

            // Excluded on purpose: every phone/email, tax and registration numbers, website,
            // legal and collection addresses, marketing/accounts contacts, document file names.
            return new
            {
                d.Id,
                d.Status,
                d.SubmissionSource,
                d.CreatedAt,
                Company = new
                {
                    Name = d.Company.CompanyName,
                    Type = d.Company.CompanyType.Name,
                    LegalEntityType = d.Company.EntityType.Name,
                },
                PrimaryContact = new { d.PrimaryContact.Name, d.PrimaryContact.JobTitle },
                Donations = new
                {
                    Frequency = d.Donations.Frequency.Name,
                    Types = d.Donations.Types.Select(t => t.Name).ToList(),
                    OperationalRegions = d.Donations.OperationalRegions.Select(r => r.Name).ToList(),
                },
                Crm = new
                {
                    RelationshipManager = d.Crm.RelationshipManager?.FullName,
                    d.Crm.FollowUpDate,
                },
                BbbeeStatus = d.Compliance.BbbeeStatus?.Name,
                DocumentCount = d.Compliance.Documents.Count(doc => doc.IsActive),
            };
        });

    [Description("A donor's latest 20 interactions, newest first.")]
    public Task<object> GetDonorInteractionsAsync(
        [Description("Donor id.")] Guid donorId,
        [Description("One of: Note, Email, Call, Meeting, FormSubmission, FollowUp.")] string? interactionType = null,
        CancellationToken cancellationToken = default) =>
        RunAsync(async () =>
        {
            var result = await sender.Send(new GetDonorInteractionsQuery
            {
                DonorId = donorId,
                InteractionType = interactionType,
                PageSize = InteractionLimit,
            }, cancellationToken);

            // shortcut: summaries are free text and may still name people or hold phone numbers;
            // decide for production during POPIA sign-off (spec Q3).
            return result.Data.Select(i => new
            {
                Type = i.InteractionType,
                OccurredAt = i.CreatedAt,
                LoggedBy = i.CreatedBy.FullName,
                Summary = Truncate(Scrub(i.Body), MaxSummaryLength),
            }).ToList();
        });

    [Description("A donor's tasks. Returns at most 25 plus totalCount.")]
    public Task<object> GetDonorTasksAsync(
        [Description("Donor id.")] Guid donorId,
        [Description("One of: all, true (completed only), false (open only).")] string isCompleted = "all",
        CancellationToken cancellationToken = default) =>
        RunAsync(async () => Tasks(await sender.Send(new GetDonorTasksQuery
        {
            DonorId = donorId,
            IsCompleted = isCompleted,
            PageSize = PageSize,
        }, cancellationToken)));

    [Description("Tasks assigned to the current user. Returns at most 25 plus totalCount.")]
    public Task<object> GetMyTasksAsync(
        [Description("true for completed tasks, false (default) for open tasks.")] bool isCompleted = false,
        [Description("Only tasks due on or before this date (yyyy-MM-dd).")] DateOnly? dueBefore = null,
        CancellationToken cancellationToken = default) =>
        RunAsync(async () => Tasks(await sender.Send(new GetMyTasksQuery
        {
            IsCompleted = isCompleted,
            DueBefore = dueBefore,
            PageSize = PageSize,
        }, cancellationToken)));

    [Description("The donor approval queue, newest first. Returns at most 25 plus totalCount.")]
    public Task<object> ListApprovalsAsync(
        [Description("One of: Pending (default), Approved, Rejected.")] string status = "Pending",
        CancellationToken cancellationToken = default) =>
        RunAsync(async () =>
        {
            var result = await sender.Send(new GetApprovalsQuery { Status = status, PageSize = PageSize }, cancellationToken);
            return new
            {
                result.Pagination.TotalCount,
                Items = result.Data.Select(a => new
                {
                    DonorId = a.Donor.Id,
                    a.Donor.CompanyName,
                    a.Status,
                    SubmittedAt = a.CreatedAt,
                    ActionedBy = a.ReviewedBy?.FullName,
                }).ToList(),
            };
        });

    [Description("Headline CRM counters: total/active donors, pending approvals, the caller's open tasks and overdue follow-ups, donors contacted this and last month.")]
    public Task<object> GetDashboardStatsAsync(CancellationToken cancellationToken = default) =>
        RunAsync(async () => await sender.Send(new GetDashboardStatsQuery(), cancellationToken));

    [Description("Donors contacted per staff member over a rolling window.")]
    public Task<object> GetManagerActivityAsync(
        [Description("One of: weekly, monthly.")] string period = "monthly",
        CancellationToken cancellationToken = default) =>
        RunAsync(async () => await sender.Send(new GetManagerActivityQuery(period), cancellationToken));

    [Description("Run an aggregate report. by_status, by_region and by_type are current snapshots (dates ignored). contacted counts donors contacted in a period per relationship manager.")]
    public Task<object> RunReportAsync(
        [Description("One of: by_status, by_region, by_type, contacted.")] string report,
        [Description("contacted only: period start (yyyy-MM-dd).")] DateOnly? startDate = null,
        [Description("contacted only: period end (yyyy-MM-dd).")] DateOnly? endDate = null,
        [Description("contacted only: limit to one relationship manager id.")] Guid? relationshipManagerId = null,
        CancellationToken cancellationToken = default) =>
        RunAsync(async () => report switch
        {
            "by_status" => await sender.Send(new GetDonorsByStatusReportQuery(), cancellationToken),
            "by_region" => await sender.Send(new GetDonorsByRegionReportQuery(), cancellationToken),
            "by_type" => await sender.Send(new GetDonorsByTypeReportQuery(), cancellationToken),
            "contacted" => await sender.Send(new GetDonorsContactedReportQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                RelationshipManagerId = relationshipManagerId,
            }, cancellationToken),
            _ => new { error = "report must be one of: by_status, by_region, by_type, contacted." },
        });

    [Description("List relationship managers (id and full name). Use it to resolve a person's name to an id.")]
    public Task<object> ListRelationshipManagersAsync(CancellationToken cancellationToken = default) =>
        RunAsync(async () => (await sender.Send(new GetRelationshipManagersQuery(), cancellationToken))
            .Select(m => new { m.Id, m.FullName }).ToList());

    private static object Tasks(Common.Models.PaginatedResult<TaskDto> result) => new
    {
        result.Pagination.TotalCount,
        Items = result.Data.Select(t => new
        {
            Title = Scrub(t.Title),
            t.DueDate,
            t.IsCompleted,
            Donor = t.Donor.CompanyName,
            Assignee = t.AssignedTo.FullName,
        }).ToList(),
    };

    // Removes URLs and email addresses from free text typed by staff or the public form.
    private static string Scrub(string text) => EmailOrUrl().Replace(text, "[removed]");

    private static string Truncate(string text, int max) => text.Length > max ? text[..max] + "…" : text;

    [GeneratedRegex(@"[\w.+-]+@[\w-]+(\.[\w-]+)+|(https?://|www\.)\S+", RegexOptions.IgnoreCase)]
    private static partial Regex EmailOrUrl();

    // Expected failures go back to the model so it can recover ("no donor with that id").
    // Anything else is reported to the model as a failed call by FunctionInvokingChatClient.
    private static async Task<object> RunAsync(Func<Task<object>> tool)
    {
        try
        {
            return await tool();
        }
        catch (NotFoundException ex)
        {
            return new { error = ex.Message };
        }
        catch (ValidationException ex)
        {
            return new { error = string.Join(" ", ex.Errors.SelectMany(e => e.Value)) };
        }
    }
}
