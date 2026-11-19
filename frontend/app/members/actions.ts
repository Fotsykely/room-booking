"use server";

import { redirect } from "next/navigation";
import { createMember, updateMember } from "@/lib/api-client/members";
import { toFormState } from "@/lib/form-errors";
import type { FormState } from "@/lib/form-state";
import { MEMBER_FIELDS, readMemberFormValues, toMemberInput } from "@/lib/member-form";
import { revalidateAllPages } from "@/lib/revalidate";

// Thin actions, same principle as app/rooms/actions.ts.

export async function createMemberAction(_previousState: FormState, formData: FormData): Promise<FormState> {
  const values = readMemberFormValues(formData);

  try {
    await createMember(toMemberInput(values));
  } catch (error) {
    return toFormState(error, MEMBER_FIELDS, values);
  }

  revalidateAllPages();
  redirect("/members");
}

// Bound to the member id by the edit page: updateMemberAction.bind(null, member.id).
export async function updateMemberAction(
  memberId: number,
  _previousState: FormState,
  formData: FormData,
): Promise<FormState> {
  const values = readMemberFormValues(formData);

  try {
    await updateMember(memberId, toMemberInput(values));
  } catch (error) {
    return toFormState(error, MEMBER_FIELDS, values);
  }

  revalidateAllPages();
  redirect("/members");
}
