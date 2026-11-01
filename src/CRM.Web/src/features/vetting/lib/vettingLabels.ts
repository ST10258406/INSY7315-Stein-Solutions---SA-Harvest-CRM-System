import type {
  EvidencePhotoCategory,
  SiteVisitStatus,
  VettingApprovalState,
  VettingDecision,
  VettingFlagSource,
} from '../types';

// The API sends C# enum names; these are the display labels from the design doc.

export const SITE_VISIT_STATUS_LABELS: Record<SiteVisitStatus, string> = {
  ToDo: 'To do',
  InProgress: 'In progress',
  Completed: 'Completed',
};

export const APPROVAL_STATE_LABELS: Record<VettingApprovalState, string> = {
  New: 'New',
  Ready: 'Ready for review',
  Returned: 'Returned',
  Approved: 'Approved',
  Declined: 'Declined',
};

export const DECISION_LABELS: Record<VettingDecision, string> = {
  Approve: 'Approve',
  Return: 'Return for corrections',
  Decline: 'Decline',
};

export const FLAG_SOURCE_LABELS: Record<VettingFlagSource, string> = {
  Auto: 'Automatic',
  Reviewer: 'Reviewer',
};

export const PHOTO_CATEGORY_LABELS: Record<EvidencePhotoCategory, string> = {
  Kitchen: 'Kitchen',
  DryStorage: 'Dry storage',
  FridgeFreezer: 'Fridge / freezer',
  ToiletsWater: 'Toilets & water',
};
