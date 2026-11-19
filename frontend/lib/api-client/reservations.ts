import { apiFetch, jsonRequest } from "./client";
import type { Reservation, ReservationFilters, ReservationInput } from "./types";

// One small function per endpoint of ReservationsController. The API has no DELETE:
// a reservation is cancelled, which keeps it in the database.

// The planning: confirmed reservations sorted by start, unless the filters say otherwise.
export function listReservations(filters: ReservationFilters = {}): Promise<Reservation[]> {
  const query = new URLSearchParams();
  if (filters.roomId !== undefined) query.set("roomId", String(filters.roomId));
  if (filters.date) query.set("date", filters.date);
  if (filters.includeCancelled) query.set("includeCancelled", "true");

  const queryString = query.toString();
  return apiFetch(queryString ? `/api/reservations?${queryString}` : "/api/reservations");
}

export function createReservation(input: ReservationInput): Promise<Reservation> {
  return apiFetch("/api/reservations", jsonRequest("POST", input));
}

export function cancelReservation(id: number): Promise<Reservation> {
  return apiFetch(`/api/reservations/${id}/cancel`, { method: "POST" });
}
