// Placeholder shown while an edit page loads its data. The number of fields matches the real form,
// so the page does not jump when the form arrives.
export function FormSkeleton({ fields }: { fields: number }) {
  return (
    <div aria-busy="true" aria-live="polite">
      <span className="sr-only">Chargement…</span>
      <div aria-hidden className="flex max-w-xl flex-col gap-5">
        {Array.from({ length: fields }, (_, index) => (
          <div key={index} className="h-16 rounded-md bg-border" />
        ))}
        <div className="h-11 w-40 rounded-md bg-border" />
      </div>
    </div>
  );
}
