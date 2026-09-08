import { StickyNote, Phone, Mail, Users, FileText, Clock, MessageSquare } from 'lucide-react';

interface InteractionTypeIconProps {
  /** Raw backend interaction type string (e.g. "Call", "FollowUp"). */
  type: string;
  className?: string;
}

/** The lucide icon for an interaction type. A direct switch (rather than a
 * component looked up from a map) keeps `react-hooks/static-components` happy. */
export function InteractionTypeIcon({ type, className }: InteractionTypeIconProps) {
  switch (type) {
    case 'Note':
      return <StickyNote className={className} />;
    case 'Call':
      return <Phone className={className} />;
    case 'Email':
      return <Mail className={className} />;
    case 'Meeting':
      return <Users className={className} />;
    case 'FormSubmission':
      return <FileText className={className} />;
    case 'FollowUp':
      return <Clock className={className} />;
    default:
      return <MessageSquare className={className} />;
  }
}
