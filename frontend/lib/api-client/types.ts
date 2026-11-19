// These types mirror the DTOs of the ASP.NET Core API (backend/Api/Dtos).
// When a DTO changes, update the matching type here.

export const SUBSCRIPTION_TYPES = ["Standard", "Premium"] as const;
export type SubscriptionType = (typeof SUBSCRIPTION_TYPES)[number];

export interface Room {
  id: number;
  name: string;
  capacity: number;
  floor: number; // 0 is the ground floor
  equipment: string | null;
  isActive: boolean;
  // True when the room is active and no confirmed reservation covers the current time.
  isAvailableNow: boolean;
}

// Request body. An empty equipment text is turned into "no equipment" by the API.
export type RoomInput = {
  name: string;
  capacity: number;
  floor: number;
  equipment: string;
  isActive: boolean;
};

export interface Member {
  id: number;
  name: string;
  email: string;
  subscription: SubscriptionType;
}

export type MemberInput = {
  name: string;
  email: string;
  subscription: SubscriptionType;
};

export type ReservationStatus = "Confirmed" | "Cancelled";

export interface Reservation {
  id: number;
  roomId: number;
  roomName: string;
  memberId: number;
  memberName: string;
  // Local times of the coworking space, without offset: "2026-11-20T14:00:00".
  startsAt: string;
  endsAt: string;
  purpose: string;
  status: ReservationStatus;
  createdAt: string;
}

// Request body. A blank date is sent as null: the API then answers "La date de début est obligatoire.",
// whereas an empty text cannot be read as a date and gives a technical error.
export type ReservationInput = {
  memberId: number;
  roomId: number;
  startsAt: string | null;
  endsAt: string | null;
  purpose: string;
};

// Optional filters of the planning. "date" is a day ("2026-11-20"): the bookings that start that day.
export type ReservationFilters = {
  roomId?: number;
  date?: string;
  includeCancelled?: boolean;
};

// Validation messages per form field, with camelCase field names: { name: ["Le nom est obligatoire."] }
export type FieldErrors = Record<string, string[]>;
