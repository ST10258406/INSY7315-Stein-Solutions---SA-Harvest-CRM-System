import { Outlet } from 'react-router-dom';
import { TopBar } from './TopBar';
import { Sidebar } from './Sidebar';

/**
 * Persistent authenticated shell: top bar (primary nav) + icon rail
 * (secondary quick actions) + routed page content. Every protected route
 * renders inside this layout via the parent layout route in App.tsx.
 */
export function AppLayout() {
  return (
    <div className="flex h-screen flex-col overflow-hidden bg-[#0C0C0A] text-[#F4F4EE]">
      <TopBar />
      <div className="flex min-h-0 flex-1">
        <Sidebar />
        <main className="min-w-0 flex-1 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
