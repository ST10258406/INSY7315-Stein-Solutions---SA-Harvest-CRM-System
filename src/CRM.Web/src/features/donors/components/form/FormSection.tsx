import type { ReactNode } from 'react';
import type { LucideIcon } from 'lucide-react';

interface FormSectionProps {
  id?: string;
  title: string;
  description?: string;
  icon?: LucideIcon;
  children: ReactNode;
}

export function FormSection({ id, title, description, icon: Icon, children }: FormSectionProps) {
  return (
    <section id={id} className="rounded-2xl border border-border bg-muted/40 p-5">
      <div className="mb-4 flex items-center gap-2.75">
        {Icon && (
          <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl border border-border bg-card text-muted-foreground">
            <Icon className="h-4 w-4" />
          </div>
        )}
        <div>
          <h3 className="m-0 text-base font-bold tracking-tight text-foreground">{title}</h3>
          {description && <p className="m-0 mt-0.5 text-xs font-medium text-muted-foreground">{description}</p>}
        </div>
      </div>
      <div className="grid grid-cols-1 gap-x-6 gap-y-4 rounded-xl border border-border bg-card p-5 shadow-sm sm:grid-cols-2">
        {children}
      </div>
    </section>
  );
}
