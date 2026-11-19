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

// Validation messages per form field, with camelCase field names: { name: ["Le nom est obligatoire."] }
export type FieldErrors = Record<string, string[]>;
