import { Link } from 'react-router-dom';
import { paths } from '@/routes/paths';

export function NotAuthorizedPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-2 text-center">
      <h1 className="text-xl font-semibold">You don't have access to this page</h1>
      <Link to={paths.dashboard} className="text-sm text-primary underline">
        Back to dashboard
      </Link>
    </div>
  );
}
