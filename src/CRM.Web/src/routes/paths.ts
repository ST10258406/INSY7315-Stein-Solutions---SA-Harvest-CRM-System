export const paths = {
  root: '/',
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
