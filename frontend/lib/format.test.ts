import { describe, expect, it } from "vitest";

import { durationInMinutes, formatCapacity, formatDay, formatDuration, formatFloor, formatPeriod, formatTime, getRoomStatus } from "./format";

describe("room status", () => {
  it("is inactive whatever the availability, because an inactive room cannot be booked", () => {
    expect(getRoomStatus({ isActive: false, isAvailableNow: false })).toBe("inactive");
    expect(getRoomStatus({ isActive: false, isAvailableNow: true })).toBe("inactive");
  });

  it("is free or busy for an active room", () => {
    expect(getRoomStatus({ isActive: true, isAvailableNow: true })).toBe("free");
    expect(getRoomStatus({ isActive: true, isAvailableNow: false })).toBe("busy");
  });
});

describe("formatFloor", () => {
  it.each([
    [0, "Rez-de-chaussée"],
    [1, "1er étage"],
    [2, "2e étage"],
    [12, "12e étage"],
    [-1, "Sous-sol 1"],
    [-2, "Sous-sol 2"],
  ])("writes floor %i as %s", (floor, expected) => {
    expect(formatFloor(floor)).toBe(expected);
  });
});

describe("dates and times", () => {
  it("reads the time from the text, without converting anything", () => {
    expect(formatTime("2026-11-20T14:05:00")).toBe("14:05");
    expect(formatTime("2026-11-20T09:00:00.123")).toBe("09:00");
  });

  it("writes a day in full, from a day or from a date and time", () => {
    expect(formatDay("2026-11-20")).toBe("vendredi 20 novembre 2026");
    expect(formatDay("2026-11-20T23:30:00")).toBe("vendredi 20 novembre 2026");
  });

  it("writes the period of a booking within one day as two times", () => {
    expect(formatPeriod("2026-11-20T14:00:00", "2026-11-20T15:30:00")).toBe("14:00 – 15:30");
  });

  it("adds the end day when the booking runs past midnight", () => {
    expect(formatPeriod("2026-11-20T23:00:00", "2026-11-21T01:00:00")).toBe("23:00 – 01:00 (le 21 nov. 2026)");
  });
});

describe("durations", () => {
  it("counts the minutes between two times of the same day", () => {
    expect(durationInMinutes("2026-11-20T14:00:00", "2026-11-20T15:30:00")).toBe(90);
  });

  it("counts across midnight", () => {
    expect(durationInMinutes("2026-11-20T23:00:00", "2026-11-21T01:00:00")).toBe(120);
  });

  it.each([
    [5, "5 min"],
    [45, "45 min"],
    [60, "1 h"],
    [90, "1 h 30"],
    [120, "2 h"],
    [125, "2 h 05"],
  ])("writes %i minutes as %s", (minutes, expected) => {
    expect(formatDuration(minutes)).toBe(expected);
  });
});

describe("formatCapacity", () => {
  it("uses the singular for one person only", () => {
    expect(formatCapacity(1)).toBe("1 personne");
    expect(formatCapacity(8)).toBe("8 personnes");
  });
});
