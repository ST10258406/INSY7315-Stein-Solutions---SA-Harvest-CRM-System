import { Outlet, Route } from 'react-router-dom';
import { ProtectedRoute } from '@/features/auth/components/ProtectedRoute';
import { paths } from '@/routes/paths';
import EditVettingPage from './pages/EditVettingPage';
import NewVettingPage from './pages/NewVettingPage';
import OfficersPage from './pages/OfficersPage';
import SiteVisitsPage from './pages/SiteVisitsPage';
import VettingDetailPage from './pages/VettingDetailPage';
import VettingListPage from './pages/VettingListPage';
import { VettingPlaceholderPage } from './pages/VettingPlaceholderPage';

const ADMIN = ['Admin', 'SuperAdmin'];
const PROCUREMENT_OR_ABOVE = ['Procurement', ...ADMIN];

/**
 * Vetting routes, rendered inside App's authenticated AppLayout route. Procurement and above
 * (Marketing is redirected); the review queue and officers are Admin only. The static paths are
 * declared before vetting/:id so it can't swallow them. The review queue is a placeholder until #164.
 */
export const vettingRoutes = (
  <>
    <Route
      path={paths.vettingReview}
      element={
        <ProtectedRoute allowedRoles={ADMIN}>
          <VettingPlaceholderPage title="Vetting review queue" />
        </ProtectedRoute>
      }
    />
    <Route
      path={paths.vettingOfficers}
      element={
        <ProtectedRoute allowedRoles={ADMIN}>
          <OfficersPage />
        </ProtectedRoute>
      }
    />
    <Route
      element={
        <ProtectedRoute allowedRoles={PROCUREMENT_OR_ABOVE}>
          <Outlet />
        </ProtectedRoute>
      }
    >
      <Route path={paths.vettingVisits} element={<SiteVisitsPage />} />
      <Route path={paths.vetting} element={<VettingListPage />} />
      <Route path={paths.vettingNew} element={<NewVettingPage />} />
      <Route path={paths.vettingEditPattern} element={<EditVettingPage />} />
      <Route path={paths.vettingDetailPattern} element={<VettingDetailPage />} />
    </Route>
  </>
);
