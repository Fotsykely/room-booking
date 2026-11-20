import type { Room, SubscriptionType } from "./api-client/types";

// Texts shown to users stay in French; identifiers and comments stay in English.

export const SUBSCRIPTION_LABELS: Record<SubscriptionType, string> = {
  Standard: "Standard",
  Premium: "Premium",
};

// What a room is right now: inactive rooms cannot be booked, the others are free or busy.
export type RoomStatus = "free" | "busy" | "inactive";

export const ROOM_STATUS_LABELS: Record<RoomStatus, string> = {
  free: "Libre",
  busy: "Occupée",
  inactive: "Inactive",
};

export function getRoomStatus(room: Pick<Room, "isActive" | "isAvailableNow">): RoomStatus {
  if (!room.isActive) return "inactive";
  return room.isAvailableNow ? "free" : "busy";
}

export function formatCapacity(capacity: number): string {
  return capacity === 1 ? "1 personne" : `${capacity} personnes`;
}

export function formatFloor(floor: number): string {
  if (floor === 0) return "Rez-de-chaussée";
  if (floor < 0) return `Sous-sol ${-floor}`;
  return floor === 1 ? "1er étage" : `${floor}e étage`;
}

const dayFormatter = new Intl.DateTimeFormat("fr-FR", { dateStyle: "full" });

// "2026-11-20" or "2026-11-20T14:00:00" -> "vendredi 20 novembre 2026". Only the day is read: the API sends
// local times without offset, so nothing is converted.
export function formatDay(isoDate: string): string {
  return dayFormatter.format(new Date(`${isoDate.slice(0, 10)}T00:00:00`));
}

// "2026-11-20T14:00:00" -> "14:00", read from the text for the same reason.
export function formatTime(isoDateTime: string): string {
  return isoDateTime.slice(11, 16);
}

// "14:00 – 15:00", with the end day added when the booking runs past midnight.
export function formatPeriod(startsAt: string, endsAt: string): string {
  const period = `${formatTime(startsAt)} – ${formatTime(endsAt)}`;
  return startsAt.slice(0, 10) === endsAt.slice(0, 10) ? period : `${period} (le ${formatDate(endsAt)})`;
}

//  Minutes between two local times
export function durationInMinutes(startsAt: string, endsAt: string): number {
  return Math.round((new Date(endsAt).getTime() - new Date(startsAt).getTime()) / 60_000);
}

// 45 -> "45 min", 120 -> "2 h", 90 -> "1 h 30", 125 -> "2 h 05"
export function formatDuration(totalMinutes: number): string {
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;

  if (hours === 0) return `${minutes} min`;
  if (minutes === 0) return `${hours} h`;
  return `${hours} h ${String(minutes).padStart(2, "0")}`;
}

const dateFormatter = new Intl.DateTimeFormat("fr-FR", { dateStyle: "medium" });

// Accepts a full ISO date and time ("2026-11-17T15:39:33Z") or a plain date ("2026-12-31").
export function formatDate(isoDate: string): string {
  return dateFormatter.format(new Date(isoDate.length === 10 ? `${isoDate}T00:00:00` : isoDate));
}
