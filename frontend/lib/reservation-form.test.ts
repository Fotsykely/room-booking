import { describe, expect, it } from "vitest";
import type { Member, Room } from "./api-client/types";
import {
  NEW_RESERVATION_VALUES,
  preselectRoom,
  readReservationFormValues,
  toMemberOptions,
  toReservationInput,
  toRoomOptions,
} from "./reservation-form";

function formDataOf(fields: Record<string, string>): FormData {
  const formData = new FormData();
  for (const [name, value] of Object.entries(fields)) formData.set(name, value);
  return formData;
}

function room(overrides: Partial<Room>): Room {
  return { id: 1, name: "Salle A", capacity: 4, floor: 0, equipment: null, isActive: true, isAvailableNow: true, ...overrides };
}

describe("reservation form", () => {
  it("turns the chosen ids into numbers and keeps the dates as typed", () => {
    const values = readReservationFormValues(
      formDataOf({ memberId: "4", roomId: "2", startsAt: "2026-11-20T14:00", endsAt: "2026-11-20T15:30", purpose: "Démo" }),
    );

    expect(toReservationInput(values)).toEqual({
      memberId: 4,
      roomId: 2,
      startsAt: "2026-11-20T14:00",
      endsAt: "2026-11-20T15:30",
      purpose: "Démo",
    });
  });

  it("sends id 0 when nothing is chosen, which the API refuses with its own message", () => {
    const input = toReservationInput(NEW_RESERVATION_VALUES);

    expect(input.memberId).toBe(0);
    expect(input.roomId).toBe(0);
  });

  it("sends null for a blank date, because an empty text cannot be read as a date by the API", () => {
    const input = toReservationInput({ ...NEW_RESERVATION_VALUES, startsAt: "", endsAt: "  " });

    expect(input.startsAt).toBeNull();
    expect(input.endsAt).toBeNull();
  });

  it("keeps a missing field empty so that the API can refuse it", () => {
    expect(readReservationFormValues(formDataOf({}))).toEqual(NEW_RESERVATION_VALUES);
  });
});

describe("select options", () => {
  it("offers only the active rooms, with their capacity", () => {
    const options = toRoomOptions([
      room({ id: 1, name: "Salle Baobab", capacity: 4 }),
      room({ id: 3, name: "Salle Vanille", isActive: false }),
      room({ id: 4, name: "Salle Zébu", capacity: 1 }),
    ]);

    expect(options).toEqual([
      { value: "1", label: "Salle Baobab (4 personnes)" },
      { value: "4", label: "Salle Zébu (1 personne)" },
    ]);
  });

  it("shows the subscription of each member", () => {
    const members: Member[] = [
      { id: 2, name: "Tiana Rabe", email: "tiana@example.com", subscription: "Premium" },
      { id: 5, name: "Lalaina Andria", email: "lalaina@example.com", subscription: "Standard" },
    ];

    expect(toMemberOptions(members)).toEqual([
      { value: "2", label: "Tiana Rabe (Premium)" },
      { value: "5", label: "Lalaina Andria (Standard)" },
    ]);
  });
});

describe("preselectRoom", () => {
  const options = [{ value: "1", label: "A" }, { value: "4", label: "B" }];

  it("keeps a room id from the address when that room can be booked", () => {
    expect(preselectRoom("4", options)).toBe("4");
  });

  it.each([["3"], ["abc"], [""], [undefined], [["1", "4"]]])(
    "chooses nothing for %j (unknown, inactive, missing or repeated)",
    (param) => {
      expect(preselectRoom(param, options)).toBe("");
    },
  );
});
