// Texts shown to users stay in French; identifiers and comments stay in English.

const dateFormatter = new Intl.DateTimeFormat("fr-FR", { dateStyle: "medium" });

// Accepts a full ISO date and time ("2026-11-17T15:39:33Z") or a plain date ("2026-12-31").
export function formatDate(isoDate: string): string {
  return dateFormatter.format(new Date(isoDate.length === 10 ? `${isoDate}T00:00:00` : isoDate));
}
