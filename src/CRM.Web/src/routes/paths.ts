export const paths = {
  root: '/',
  // Public donor onboarding form — unauthenticated, shared externally (marketing
  // material, email signatures). Confirm this exact path with the team before
  // it's printed anywhere, since changing it later breaks external links.
  publicDonorForm: '/donate',
  login: '/login',
  dashboard: '/dashboard',
  donors: '/donors',
  donorNew: '/donors/new',
  // Route pattern for <Route path>. Use donorDetail(id) to build a link href.
  donorDetailPattern: '/donors/:id',
  donorDetail: (id: string) => `/donors/${id}`,
  donorEditPattern: '/donors/:id/edit',
  donorEdit: (id: string) => `/donors/${id}/edit`,
  tasks: '/tasks',
  approvals: '/approvals',
  reports: '/reports',
  users: '/users',
  notAuthorized: '/not-authorized',
  forgotPassword: '/forgot-password',
  resetPassword: '/reset-password',
};
