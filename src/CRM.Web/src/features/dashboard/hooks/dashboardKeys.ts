/** Query-key factory for dashboard data. */
export const dashboardKeys = {
  all: ['dashboard'] as const,
  stats: () => [...dashboardKeys.all, 'stats'] as const,
  managerActivity: (period: string) => [...dashboardKeys.all, 'manager-activity', period] as const,
};
