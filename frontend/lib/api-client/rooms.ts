import { apiFetch, jsonRequest } from "./client";
import type { Room, RoomInput } from "./types";

// One small function per endpoint of RoomsController. Pages and Server Actions call these
// and never deal with fetch or status codes. The API has no DELETE: a room is deactivated instead.

export function listRooms(): Promise<Room[]> {
  return apiFetch("/api/rooms");
}

export function getRoom(id: number): Promise<Room> {
  return apiFetch(`/api/rooms/${id}`);
}

export function createRoom(input: RoomInput): Promise<Room> {
  return apiFetch("/api/rooms", jsonRequest("POST", input));
}

export function updateRoom(id: number, input: RoomInput): Promise<Room> {
  return apiFetch(`/api/rooms/${id}`, jsonRequest("PUT", input));
}
