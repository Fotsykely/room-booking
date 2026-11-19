import type { Reservation, ReservationFilters } from "./api-client/types";

// The address of the planning carries its filters: /planning?roomId=2&date=2026-11-20&includeCancelled=true
type SearchParams = Record<string, string | string[] | undefined>;

// The address is typed by users: a value that cannot be a filter is ignored rather than sent to the API,
// which would answer with an error page.
export function parsePlanningFilters(searchParams: SearchParams): ReservationFilters {
  const roomId = Number(single(searchParams.roomId));
  const date = single(searchParams.date);

  return {
    roomId: Number.isInteger(roomId) && roomId > 0 ? roomId : undefined,
    date: date !== undefined && isCalendarDay(date) ? date : undefined,
    includeCancelled: single(searchParams.includeCancelled) === "true",
  };
}

export type DayGroup = { day: string; reservations: Reservation[] };

// Groups consecutive reservations by the day they start. The API already sorts them by start.
export function groupByDay(reservations: Reservation[]): DayGroup[] {
  const groups: DayGroup[] = [];

  for (const reservation of reservations) {
    const day = reservation.startsAt.slice(0, 10);
    const last = groups.at(-1);

    if (last?.day === day) last.reservations.push(reservation);
    else groups.push({ day, reservations: [reservation] });
  }

  return groups;
}

// A repeated parameter (?date=a&date=b) arrives as an array: only the first value counts.
function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// "2026-02-31" has the right shape but is not a day: the date rolls over to March, so the round trip differs.
function isCalendarDay(text: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(text)) return false;
  const parsed = new Date(`${text}T00:00:00Z`);
  return !Number.isNaN(parsed.getTime()) && parsed.toISOString().startsWith(text);
}
