"use server";

import { redirect } from "next/navigation";
import { cancelReservation, createReservation } from "@/lib/api-client/reservations";
import { toFormState } from "@/lib/form-errors";
import type { FormState } from "@/lib/form-state";
import { RESERVATION_FIELDS, readReservationFormValues, toReservationInput } from "@/lib/reservation-form";
import { revalidateAllPages } from "@/lib/revalidate";

// Thin actions, same principle as app/rooms/actions.ts. The booking rules (overlap, 4 hours, limit of 3...) are
// all checked by the API: its messages come back under the matching field of the form.
// There is no authentication in this app (see the README): nothing to check before calling the API.

export async function createReservationAction(_previousState: FormState, formData: FormData): Promise<FormState> {
  const values = readReservationFormValues(formData);

  try {
    await createReservation(toReservationInput(values));
  } catch (error) {
    return toFormState(error, RESERVATION_FIELDS, values);
  }

  revalidateAllPages();
  redirect("/planning");
}

// Bound to the reservation id by the planning: cancelReservationAction.bind(null, reservation.id).
// No redirect: the planning refreshes where it is, so the filters the user chose are kept.
export async function cancelReservationAction(reservationId: number): Promise<void> {
  await cancelReservation(reservationId);
  revalidateAllPages();
}
