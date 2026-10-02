import { Outlet } from 'react-router-dom';
import { TopBar } from './TopBar';

/**
 * Persistent authenticated shell: top bar (primary nav) + routed page content.
 * Every protected route renders inside this layout via the parent layout route
 * in App.tsx.
 *
 * The quick-action icon rail (./Sidebar) is deliberately not rendered for
 * release — none of its actions (search, saved filters, calendar, agent, help)
 * are implemented yet. Re-add `<Sidebar />` before `<main>` once they are.
 */
export function AppLayout() {
  return (
    <div className="flex h-screen flex-col overflow-hidden bg-background text-foreground">
      <TopBar />
      <div className="flex min-h-0 flex-1">
        <main className="min-w-0 flex-1 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
