import { describe, expect, it } from "vitest";
import type { Reservation } from "./api-client/types";
import { groupByDay, parsePlanningFilters } from "./planning";

function reservation(id: number, startsAt: string): Reservation {
  return {
    id, roomId: 1, roomName: "Salle A", memberId: 1, memberName: "Hery", startsAt, endsAt: startsAt,
    purpose: "Test", status: "Confirmed", createdAt: "2026-11-01T08:00:00",
  };
}

describe("parsePlanningFilters", () => {
  it("reads the three filters from the address", () => {
    const filters = parsePlanningFilters({ roomId: "2", date: "2026-11-20", includeCancelled: "true" });

    expect(filters).toEqual({ roomId: 2, date: "2026-11-20", includeCancelled: true });
  });

  it("applies no filter when the address has none", () => {
    expect(parsePlanningFilters({})).toEqual({ roomId: undefined, date: undefined, includeCancelled: false });
  });

  it("ignores the empty values that a submitted filter form sends", () => {
    const filters = parsePlanningFilters({ roomId: "", date: "" });

    expect(filters.roomId).toBeUndefined();
    expect(filters.date).toBeUndefined();
  });

  it.each([["abc"], ["0"], ["-3"], ["1.5"]])("ignores the room id %j", (roomId) => {
    expect(parsePlanningFilters({ roomId }).roomId).toBeUndefined();
  });

  it.each([["20/11/2026"], ["2026-11"], ["2026-13-01"], ["2026-02-31"], ["not-a-date"]])(
    "ignores the date %j, which the API would refuse with an error page",
    (date) => {
      expect(parsePlanningFilters({ date }).date).toBeUndefined();
    },
  );

  it("accepts a real day such as the 29th of February of a leap year", () => {
    expect(parsePlanningFilters({ date: "2028-02-29" }).date).toBe("2028-02-29");
  });

  it("keeps the first value of a repeated parameter", () => {
    expect(parsePlanningFilters({ roomId: ["3", "5"] }).roomId).toBe(3);
  });

  it("shows the cancelled bookings only for the exact value true", () => {
    expect(parsePlanningFilters({ includeCancelled: "yes" }).includeCancelled).toBe(false);
    expect(parsePlanningFilters({ includeCancelled: "true" }).includeCancelled).toBe(true);
  });
});

describe("groupByDay", () => {
  it("groups consecutive reservations by the day they start, keeping the order", () => {
    const groups = groupByDay([
      reservation(1, "2026-11-20T09:00:00"),
      reservation(2, "2026-11-20T14:00:00"),
      reservation(3, "2026-11-21T10:00:00"),
    ]);

    expect(groups.map((group) => [group.day, group.reservations.map((r) => r.id)])).toEqual([
      ["2026-11-20", [1, 2]],
      ["2026-11-21", [3]],
    ]);
  });

  it("puts a booking that runs past midnight in its start day", () => {
    const groups = groupByDay([{ ...reservation(1, "2026-11-20T23:00:00"), endsAt: "2026-11-21T01:00:00" }]);

    expect(groups.map((group) => group.day)).toEqual(["2026-11-20"]);
  });

  it("gives no group for no reservation", () => {
    expect(groupByDay([])).toEqual([]);
  });
});
