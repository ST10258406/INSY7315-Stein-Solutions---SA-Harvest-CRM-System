/** Route placeholder until the real page lands (#161–#164). */
export function VettingPlaceholderPage({ title }: { title: string }) {
  return (
    <div className="p-6.5">
      <h1 className="m-0 text-2xl font-extrabold tracking-tight text-foreground">{title}</h1>
      <p className="mt-2 text-sm text-muted-foreground">Coming soon.</p>
    </div>
  );
}
