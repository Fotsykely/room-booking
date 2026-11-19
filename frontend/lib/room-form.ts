import type { Room, RoomInput } from "./api-client/types";
import { parseInteger, readText } from "./form-data";

// Names of the form fields, used to keep only the API messages that belong to this form.
export const ROOM_FIELDS = ["name", "capacity", "floor", "equipment", "isActive"] as const;

// What the room form holds. A form only handles text: the "active" checkbox is "on" when checked
// and "" otherwise, exactly as the browser sends it.
export type RoomFormValues = Record<(typeof ROOM_FIELDS)[number], string>;

export const NEW_ROOM_VALUES: RoomFormValues = { name: "", capacity: "", floor: "0", equipment: "", isActive: "on" };

export function readRoomFormValues(formData: FormData): RoomFormValues {
  return {
    name: readText(formData, "name"),
    capacity: readText(formData, "capacity"),
    floor: readText(formData, "floor"),
    equipment: readText(formData, "equipment"),
    isActive: readText(formData, "isActive"),
  };
}

export function toRoomInput(values: RoomFormValues): RoomInput {
  return {
    name: values.name,
    capacity: parseInteger(values.capacity),
    floor: parseInteger(values.floor),
    equipment: values.equipment,
    isActive: values.isActive === "on",
  };
}

export function roomToFormValues(room: Room): RoomFormValues {
  return {
    name: room.name,
    capacity: String(room.capacity),
    floor: String(room.floor),
    equipment: room.equipment ?? "",
    isActive: room.isActive ? "on" : "",
  };
}
