namespace CRM.Domain.Enums;

/// <summary>
/// The type of in-app notification delivered to a user.
/// </summary>
public enum NotificationType
{
    /// <summary>A donor's follow-up date has arrived (daily background job).</summary>
    FollowUpReminder,

    /// <summary>A new donor was submitted and is awaiting review.</summary>
    NewDonorPendingReview,

    /// <summary>An assigned task is due today and not yet completed (daily background job).</summary>
    TaskDue,

    /// <summary>A new task was assigned to this user.</summary>
    TaskAssigned,

    /// <summary>A donor submission this user's relationship-managed was approved.</summary>
    DonorApproved,

    /// <summary>A donor submission this user's relationship-managed was rejected.</summary>
    DonorRejected
}
