import type { Member, ReservationInput, Room } from "./api-client/types";
import { SUBSCRIPTION_LABELS, formatCapacity } from "./format";
import { parseInteger, readText } from "./form-data";

// Names of the form fields, used to keep only the API messages that belong to this form.
export const RESERVATION_FIELDS = ["memberId", "roomId", "startsAt", "endsAt", "purpose"] as const;

// What the booking form holds, as text. A <select> gives the id of the chosen option ("" for none) and a
// datetime-local input gives "2026-11-20T14:00" ("" for none).
export type ReservationFormValues = Record<(typeof RESERVATION_FIELDS)[number], string>;

export const NEW_RESERVATION_VALUES: ReservationFormValues = {
  memberId: "",
  roomId: "",
  startsAt: "",
  endsAt: "",
  purpose: "",
};

// An option of a <select>, narrowed to what the form needs (the full room or member is not sent to the browser).
export type SelectOption = { value: string; label: string };

export function readReservationFormValues(formData: FormData): ReservationFormValues {
  return {
    memberId: readText(formData, "memberId"),
    roomId: readText(formData, "roomId"),
    startsAt: readText(formData, "startsAt"),
    endsAt: readText(formData, "endsAt"),
    purpose: readText(formData, "purpose"),
  };
}

// No choice gives id 0, refused by the API with "Choisissez un membre." (or a room).
// A blank date becomes null, refused with "La date de début est obligatoire." (see ReservationInput).
export function toReservationInput(values: ReservationFormValues): ReservationInput {
  return {
    memberId: parseInteger(values.memberId),
    roomId: parseInteger(values.roomId),
    startsAt: blankToNull(values.startsAt),
    endsAt: blankToNull(values.endsAt),
    purpose: values.purpose,
  };
}

// Only active rooms can be booked: an inactive one is not offered.
export function toRoomOptions(rooms: Room[]): SelectOption[] {
  return rooms
    .filter((room) => room.isActive)
    .map((room) => ({ value: String(room.id), label: `${room.name} (${formatCapacity(room.capacity)})` }));
}

// The subscription is shown because it decides how many upcoming bookings the member may have.
export function toMemberOptions(members: Member[]): SelectOption[] {
  return members.map((member) => ({
    value: String(member.id),
    label: `${member.name} (${SUBSCRIPTION_LABELS[member.subscription]})`,
  }));
}

// A room id from the address (?roomId=3) is kept only if that room can be booked.
export function preselectRoom(roomIdParam: string | string[] | undefined, roomOptions: SelectOption[]): string {
  return typeof roomIdParam === "string" && roomOptions.some((option) => option.value === roomIdParam)
    ? roomIdParam
    : "";
}

function blankToNull(text: string): string | null {
  return text.trim() === "" ? null : text;
}
