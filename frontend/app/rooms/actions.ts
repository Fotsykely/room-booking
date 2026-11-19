"use server";

import { redirect } from "next/navigation";
import { createRoom, updateRoom } from "@/lib/api-client/rooms";
import { toFormState } from "@/lib/form-errors";
import type { FormState } from "@/lib/form-state";
import { revalidateAllPages } from "@/lib/revalidate";
import { ROOM_FIELDS, readRoomFormValues, toRoomInput } from "@/lib/room-form";

// These actions stay thin: read the form, call the API client, then redirect or return the errors.
// redirect() works by throwing, so it is always called outside the try/catch blocks.
// There is no authentication in this app (see the README): nothing to check before calling the API.

export async function createRoomAction(_previousState: FormState, formData: FormData): Promise<FormState> {
  const values = readRoomFormValues(formData);

  try {
    await createRoom(toRoomInput(values));
  } catch (error) {
    return toFormState(error, ROOM_FIELDS, values);
  }

  revalidateAllPages();
  redirect("/");
}

// Bound to the room id by the edit page: updateRoomAction.bind(null, room.id).
export async function updateRoomAction(
  roomId: number,
  _previousState: FormState,
  formData: FormData,
): Promise<FormState> {
  const values = readRoomFormValues(formData);

  try {
    await updateRoom(roomId, toRoomInput(values));
  } catch (error) {
    return toFormState(error, ROOM_FIELDS, values);
  }

  revalidateAllPages();
  redirect("/");
}
