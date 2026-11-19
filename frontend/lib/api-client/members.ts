import { apiFetch, jsonRequest } from "./client";
import type { Member, MemberInput } from "./types";

// One small function per endpoint of MembersController. The API has no DELETE:
// reservations keep pointing at their member.

export function listMembers(): Promise<Member[]> {
  return apiFetch("/api/members");
}

export function getMember(id: number): Promise<Member> {
  return apiFetch(`/api/members/${id}`);
}

export function createMember(input: MemberInput): Promise<Member> {
  return apiFetch("/api/members", jsonRequest("POST", input));
}

export function updateMember(id: number, input: MemberInput): Promise<Member> {
  return apiFetch(`/api/members/${id}`, jsonRequest("PUT", input));
}
