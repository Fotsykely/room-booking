import { describe, expect, it } from "vitest";
import type { Room } from "./api-client/types";
import { NEW_ROOM_VALUES, readRoomFormValues, roomToFormValues, toRoomInput } from "./room-form";

function formDataOf(fields: Record<string, string>): FormData {
  const formData = new FormData();
  for (const [name, value] of Object.entries(fields)) formData.set(name, value);
  return formData;
}

describe("room form", () => {
  it("reads every field as text and keeps a missing one empty", () => {
    const values = readRoomFormValues(formDataOf({ name: "Salle A", capacity: "6", floor: "2" }));

    expect(values).toEqual({ name: "Salle A", capacity: "6", floor: "2", equipment: "", isActive: "" });
  });

  it("turns the text values into the numbers and the boolean the API expects", () => {
    const input = toRoomInput({ name: "Salle A", capacity: "6", floor: "-1", equipment: "Écran", isActive: "on" });

    expect(input).toEqual({ name: "Salle A", capacity: 6, floor: -1, equipment: "Écran", isActive: true });
  });

  it("reads an unchecked checkbox, which the browser does not send, as inactive", () => {
    const values = readRoomFormValues(formDataOf({ name: "Salle A", capacity: "6", floor: "0" }));

    expect(toRoomInput(values).isActive).toBe(false);
  });

  it("sends 0 for a blank capacity so that the API refuses it with its own message", () => {
    expect(toRoomInput({ ...NEW_ROOM_VALUES, name: "Salle A" }).capacity).toBe(0);
  });

  it("sends the ground floor for a blank floor", () => {
    expect(toRoomInput({ ...NEW_ROOM_VALUES, floor: "  " }).floor).toBe(0);
  });

  it("starts a new room active, on the ground floor", () => {
    const input = toRoomInput(NEW_ROOM_VALUES);

    expect(input.isActive).toBe(true);
    expect(input.floor).toBe(0);
  });

  it("fills the edit form from a room, with no equipment as an empty text", () => {
    const room: Room = {
      id: 3, name: "Salle Vanille", capacity: 6, floor: 0, equipment: null, isActive: false, isAvailableNow: false,
    };

    expect(roomToFormValues(room)).toEqual({
      name: "Salle Vanille", capacity: "6", floor: "0", equipment: "", isActive: "",
    });
  });

  it("gives back the same room after a round trip through the form", () => {
    const room: Room = {
      id: 1, name: "Salle Baobab", capacity: 4, floor: 1, equipment: "Écran", isActive: true, isAvailableNow: true,
    };

    expect(toRoomInput(roomToFormValues(room))).toEqual({
      name: "Salle Baobab", capacity: 4, floor: 1, equipment: "Écran", isActive: true,
    });
  });
});
