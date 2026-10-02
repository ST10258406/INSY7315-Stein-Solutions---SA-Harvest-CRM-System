import { csvJoin, csvRow } from '@/lib/csv';
import type { DonorListItemDto } from '../types';

const HEADER = [
  'Company name',
  'Company type',
  'Status',
  'Relationship manager',
  'Operational regions',
  'Donation frequency',
  'Donation types',
  'Last interaction date',
  'Last interaction type',
  'Follow-up date',
  'Submission source',
];

const STATUS_LABEL: Record<string, string> = { PendingReview: 'Pending review' };

/** The donor list as a flat CSV — the same lightweight DonorListItemDto fields the table shows. */
export function buildDonorListCsv(donors: DonorListItemDto[]): string {
  return csvJoin([
    csvRow(...HEADER),
    ...donors.map((d) =>
      csvRow(
        d.companyName,
        d.companyType,
        STATUS_LABEL[d.status] ?? d.status,
        d.relationshipManager?.fullName ?? '',
        d.operationalRegions.join('; '),
        d.donationFrequency ?? '',
        d.donationTypes.join('; '),
        d.lastInteractionDate?.slice(0, 10) ?? '',
        d.lastInteractionType ?? '',
        d.followUpDate?.slice(0, 10) ?? '',
        d.submissionSource,
      ),
    ),
  ]);
}
