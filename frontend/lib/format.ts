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

const dateFormatter = new Intl.DateTimeFormat("fr-FR", { dateStyle: "medium" });

// Accepts a full ISO date and time ("2026-11-17T15:39:33Z") or a plain date ("2026-12-31").
export function formatDate(isoDate: string): string {
  return dateFormatter.format(new Date(isoDate.length === 10 ? `${isoDate}T00:00:00` : isoDate));
}
