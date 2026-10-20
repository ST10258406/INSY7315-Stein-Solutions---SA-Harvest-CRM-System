import { Outlet, Route } from 'react-router-dom';
import { ProtectedRoute } from '@/features/auth/components/ProtectedRoute';
import { paths } from '@/routes/paths';
import OfficersPage from './pages/OfficersPage';
import SiteVisitsPage from './pages/SiteVisitsPage';
import { VettingPlaceholderPage } from './pages/VettingPlaceholderPage';

const ADMIN = ['Admin', 'SuperAdmin'];
const PROCUREMENT_OR_ABOVE = ['Procurement', ...ADMIN];

/**
 * Vetting routes, rendered inside App's authenticated AppLayout route. Procurement and above
 * (Marketing is redirected); the review queue and officers are Admin only. The static paths are
 * declared before vetting/:id so it can't swallow them. Placeholders remain until #162–#164.
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
      <Route path={paths.vetting} element={<VettingPlaceholderPage title="Vetting submissions" />} />
      <Route path={paths.vettingNew} element={<VettingPlaceholderPage title="New vetting" />} />
      <Route path={paths.vettingEditPattern} element={<VettingPlaceholderPage title="Edit vetting" />} />
      <Route path={paths.vettingDetailPattern} element={<VettingPlaceholderPage title="Vetting submission" />} />
    </Route>
  </>
);
