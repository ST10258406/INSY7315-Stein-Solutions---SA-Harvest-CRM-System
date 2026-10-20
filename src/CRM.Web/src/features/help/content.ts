import { paths } from '@/routes/paths';

/**
 * Help & support content (Issue 105). Kept as data, not JSX, so it can be edited without
 * touching the drawer. `roles` mirrors the API policy for that feature; undefined means
 * every role. Role filtering here is cosmetic only — the API enforces access.
 */
export interface HelpTopic {
  id: string;
  title: string;
  steps: string[];
  path?: string;
  roles?: readonly string[];
}

export interface HelpShortcut {
  keys: string;
  description: string;
  roles?: readonly string[];
}

const PROCUREMENT_OR_ABOVE = ['Procurement', 'Admin', 'SuperAdmin'] as const;
const ADMIN_OR_ABOVE = ['Admin', 'SuperAdmin'] as const;
const SUPER_ADMIN = ['SuperAdmin'] as const;

export const HELP_TOPICS: HelpTopic[] = [
  {
    id: 'find-donors',
    title: 'Finding and filtering donors',
    path: paths.donors,
    roles: PROCUREMENT_OR_ABOVE,
    steps: [
      'Open Donors from the top bar.',
      'Type part of a company name in the search box.',
      'Use the filters for status, company type, region, donation type or follow-up date.',
      'Click a column header to sort. The filters stay in the page address, so you can bookmark or share the view.',
    ],
  },
  {
    id: 'add-donor',
    title: 'Adding a donor and sending the public form invite',
    path: paths.donorNew,
    roles: PROCUREMENT_OR_ABOVE,
    steps: [
      'On Donors, click New Donor and fill in the company, contact and donation details.',
      'Save. New donors start as Pending review until an Admin approves them.',
      'To let a donor fill in their own details, use Send public form and enter their email address.',
    ],
  },
  {
    id: 'log-interaction',
    title: 'Logging an interaction and setting a follow-up',
    path: paths.donors,
    roles: PROCUREMENT_OR_ABOVE,
    steps: [
      'Open the donor and go to the interactions section.',
      'Choose the type (call, meeting, email or note) and write a short summary.',
      'Set a follow-up date if you need to get back to them. You will get a reminder.',
      'Interactions cannot be edited or deleted once saved, so check them before saving.',
    ],
  },
  {
    id: 'tasks',
    title: 'Creating and completing tasks',
    path: paths.tasks,
    steps: [
      'Tasks shows the tasks assigned to you, soonest due first. Overdue tasks are highlighted.',
      'To create a task, open the donor it belongs to and add it from the tasks panel.',
      'Click the tick next to a task to mark it complete.',
    ],
  },
  {
    id: 'approvals',
    title: 'Reviewing donor approvals',
    path: paths.approvals,
    roles: ADMIN_OR_ABOVE,
    steps: [
      'Open Approvals to see donors waiting for review.',
      'Open a donor to check their details and documents.',
      'Approve, or reject with a reason. The decision is recorded in the audit log.',
    ],
  },
  {
    id: 'reports',
    title: 'Running and exporting reports',
    path: paths.reports,
    roles: ADMIN_OR_ABOVE,
    steps: [
      'Open Reports and choose a report.',
      'Set the date range or filters it offers.',
      'Use Export to download it as PDF or Excel.',
    ],
  },
  {
    id: 'users',
    title: 'Managing users',
    path: paths.users,
    roles: ADMIN_OR_ABOVE,
    steps: [
      'Open Users to see every staff account.',
      'Add a user with their name, email and role. They must change the temporary password at first login.',
      'Deactivate a user instead of deleting them; their history is kept.',
    ],
  },
  {
    id: 'assistant',
    title: 'Using the CRM assistant',
    roles: SUPER_ADMIN,
    steps: [
      'Click the robot icon on the left rail to open the assistant.',
      'Ask a question about donors, tasks, approvals or reports in plain English.',
      'The assistant can only read data, never change it. It shows which lookups it used under each answer.',
      'Check anything important against the CRM itself.',
    ],
  },
];

// Only shortcuts that exist today.
export const HELP_SHORTCUTS: HelpShortcut[] = [
  { keys: 'Ctrl + K (⌘K on Mac)', description: 'Quick search: jump to a donor or a page' },
  { keys: 'Enter', description: 'Send a message to the assistant', roles: SUPER_ADMIN },
  { keys: 'Shift + Enter', description: 'New line in an assistant message', roles: SUPER_ADMIN },
  { keys: 'Esc', description: 'Close a drawer or dialog' },
];

export const canSee = (item: { roles?: readonly string[] }, userRoles: readonly string[]) =>
  !item.roles || item.roles.some((role) => userRoles.includes(role));

/**
 * "Report a problem" body: page, version, browser and role only. Never names, emails,
 * donor data or tokens — the user can add detail themselves before sending.
 */
export function buildProblemReportBody(page: string, version: string, browser: string, role: string) {
  return `Page: ${page}\nApp version: ${version}\nBrowser: ${browser}\nRole: ${role}\n\nWhat happened:\n\nWhat did you expect to happen:\n`;
}
