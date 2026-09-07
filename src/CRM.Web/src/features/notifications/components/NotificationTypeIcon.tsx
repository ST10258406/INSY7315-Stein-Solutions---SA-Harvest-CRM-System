import { Clock, UserPlus, CalendarClock, ClipboardCheck, CheckCircle2, XCircle, Bell } from 'lucide-react';

interface NotificationTypeIconProps {
  type: string;
  className?: string;
}

/** Lucide icon for a notification type. A direct switch keeps the linter's
 * `react-hooks/static-components` rule happy (no component looked up from a map). */
export function NotificationTypeIcon({ type, className }: NotificationTypeIconProps) {
  switch (type) {
    case 'FollowUpReminder':
      return <Clock className={className} />;
    case 'NewDonorPendingReview':
      return <UserPlus className={className} />;
    case 'TaskDue':
      return <CalendarClock className={className} />;
    case 'TaskAssigned':
      return <ClipboardCheck className={className} />;
    case 'DonorApproved':
      return <CheckCircle2 className={className} />;
    case 'DonorRejected':
      return <XCircle className={className} />;
    default:
      return <Bell className={className} />;
  }
}
