namespace CRM.Application.Modules.Vetting.Submissions.Commands;

using CRM.Application.Common.Exceptions;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;

internal static class SubmissionGuards
{
    /// <summary>Details are editable, and the form submittable, only while with the officer.</summary>
    public static void EnsureEditable(VettingSubmission submission)
    {
        if (submission.ApprovalState is not (VettingApprovalState.New or VettingApprovalState.Returned))
            throw new ConflictException(
                $"Submission {submission.FormReference} is {submission.ApprovalState}; only New or Returned submissions can be changed.");
    }
}
