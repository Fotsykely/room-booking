import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cancelReservation, listReservations } from "./reservations";

// "server-only" throws when imported outside a Server Component: replace it with an empty module.
vi.mock("server-only", () => ({}));

function stubFetch() {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response("[]", { status: 200, headers: { "Content-Type": "application/json" } }),
  );
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

describe("reservations client", () => {
  beforeEach(() => {
    vi.stubEnv("API_BASE_URL", "http://api.test");
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
  });

  it("asks for the whole planning when there is no filter", async () => {
    const fetchMock = stubFetch();

    await listReservations();

    expect(fetchMock.mock.calls[0][0]).toBe("http://api.test/api/reservations");
  });

  it("puts the filters in the query string", async () => {
    const fetchMock = stubFetch();

    await listReservations({ roomId: 2, date: "2026-11-20", includeCancelled: true });

    expect(fetchMock.mock.calls[0][0]).toBe(
      "http://api.test/api/reservations?roomId=2&date=2026-11-20&includeCancelled=true",
    );
  });

  it("leaves out the filters that are not set, and a false includeCancelled", async () => {
    const fetchMock = stubFetch();

    await listReservations({ roomId: 5, includeCancelled: false });

    expect(fetchMock.mock.calls[0][0]).toBe("http://api.test/api/reservations?roomId=5");
  });

  it("cancels with a POST on the cancel route of the reservation", async () => {
    const fetchMock = stubFetch();

    await cancelReservation(7);

    expect(fetchMock.mock.calls[0][0]).toBe("http://api.test/api/reservations/7/cancel");
    expect(fetchMock.mock.calls[0][1]).toMatchObject({ method: "POST" });
  });
});
