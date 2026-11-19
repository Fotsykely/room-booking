import { describe, expect, it } from "vitest";
import { formatCapacity, formatFloor, getRoomStatus } from "./format";

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

describe("formatCapacity", () => {
  it("uses the singular for one person only", () => {
    expect(formatCapacity(1)).toBe("1 personne");
    expect(formatCapacity(8)).toBe("8 personnes");
  });
});
